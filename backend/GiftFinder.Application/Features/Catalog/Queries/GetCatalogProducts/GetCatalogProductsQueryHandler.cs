using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Catalog.Queries.GetCatalogProducts;

public class GetCatalogProductsQueryHandler : IRequestHandler<GetCatalogProductsQuery, CatalogProductsResultDto>
{
    private readonly IApplicationDbContext _context;

    // Cache in-memory toàn bộ danh sách sản phẩm đã duyệt (có tag) trong 5 phút để tốc độ lướt danh mục & chuyển trang đạt < 5ms, 0 token AI
    private static List<CachedCatalogProduct>? _cachedProducts;
    private static DateTime _cacheExpireAt = DateTime.MinValue;
    private static readonly object _cacheLock = new();

    private sealed class CachedCatalogProduct
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = default!;
        public string? Description { get; init; }
        public long Price { get; init; }
        public long? OriginalPrice { get; init; }
        public string? ImageUrl { get; init; }
        public string OriginalUrl { get; init; } = default!;
        public string? AffiliateUrl { get; init; }
        public ProductSource SourceEnum { get; init; }
        public string Source { get; init; } = default!;
        public double Rating { get; init; }
        public int TotalReviews { get; init; }
        public int PopularityScore { get; init; }
        public bool IsFeatured { get; init; }
        public DateTime? FeaturedUntil { get; init; }
        public bool HasSuitableZodiacs { get; init; }
        public DateTime CreatedAt { get; init; }
        public List<string> TagNames { get; init; } = new();
    }

    public GetCatalogProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public static void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedProducts = null;
            _cacheExpireAt = DateTime.MinValue;
        }
    }

    private async Task<List<CachedCatalogProduct>> GetApprovedTaggedProductsAsync(CancellationToken cancellationToken)
    {
        if (_cachedProducts != null && DateTime.UtcNow < _cacheExpireAt)
        {
            return _cachedProducts;
        }

        // BẮT BUỘC: Chỉ lấy các sản phẩm Approved VÀ đã được gắn ít nhất 1 Tag (p.ProductTags.Any())
        // 2 sản phẩm chưa có tag sẽ tự động bị loại trừ hoàn toàn
        var rawProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Approved && p.ProductTags.Any())
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.OriginalPrice,
                p.ImageUrl,
                p.OriginalUrl,
                p.AffiliateUrl,
                p.Source,
                p.Rating,
                p.TotalReviews,
                p.PopularityScore,
                p.IsFeatured,
                p.FeaturedUntil,
                p.SuitableZodiacs,
                p.CreatedAt,
                TagNames = p.ProductTags.Select(pt => pt.Tag.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        var mapped = rawProducts
            .Where(p => p.TagNames != null && p.TagNames.Count > 0)
            .Select(p => new CachedCatalogProduct
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                ImageUrl = p.ImageUrl,
                OriginalUrl = p.OriginalUrl,
                AffiliateUrl = p.AffiliateUrl,
                SourceEnum = p.Source,
                Source = p.Source.ToString(),
                Rating = p.Rating,
                TotalReviews = p.TotalReviews,
                PopularityScore = p.PopularityScore,
                IsFeatured = p.IsFeatured,
                FeaturedUntil = p.FeaturedUntil,
                HasSuitableZodiacs = p.SuitableZodiacs != null && p.SuitableZodiacs.Count > 0,
                CreatedAt = p.CreatedAt,
                TagNames = p.TagNames
            })
            .ToList();

        lock (_cacheLock)
        {
            _cachedProducts = mapped;
            _cacheExpireAt = DateTime.UtcNow.AddMinutes(5);
        }

        return mapped;
    }

    public async Task<CatalogProductsResultDto> Handle(GetCatalogProductsQuery request, CancellationToken cancellationToken)
    {
        var allProducts = await GetApprovedTaggedProductsAsync(cancellationToken);

        var slug = string.IsNullOrWhiteSpace(request.Category) ? "tat-ca" : request.Category.Trim().ToLowerInvariant();
        var rule = CatalogCategoryDefinitions.GetBySlug(slug);

        // Nếu không tìm thấy slug định nghĩa sẵn -> trả về tập rỗng, tuyệt đối không nhét bừa sản phẩm
        if (rule == null)
        {
            return new CatalogProductsResultDto
            {
                Category = new CatalogCategoryInfoDto
                {
                    Slug = slug,
                    Title = "Danh mục quà tặng",
                    Group = "Danh mục quà tặng"
                },
                Total = 0,
                Page = 1,
                PageSize = Math.Clamp(request.PageSize, 1, 60),
                TotalPages = 0,
                Items = new List<CatalogProductItemDto>()
            };
        }

        var now = DateTime.UtcNow;

        // 1. Đánh giá điểm khớp theo luật danh mục (không dùng AI, không fallback nhét bừa)
        var scoredList = new List<(CachedCatalogProduct Product, int Score)>();

        var cleanKeyword = (request.Keyword ?? "").Trim().ToLowerInvariant();
        var keywordTokens = string.IsNullOrEmpty(cleanKeyword)
            ? Array.Empty<string>()
            : cleanKeyword.Split(new[] { ' ', ',', '.', ';', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                          .Where(w => w.Length > 1 && w != "quà" && w != "tặng" && w != "tìm" && w != "mua" && w != "cho")
                          .ToArray();

        foreach (var p in allProducts)
        {
            // Lọc theo sàn (nếu có)
            if (request.Source.HasValue && p.SourceEnum != request.Source.Value)
                continue;

            // Lọc theo giá (nếu có)
            if (request.MinPrice.HasValue && p.Price < request.MinPrice.Value)
                continue;

            if (request.MaxPrice.HasValue && p.Price > request.MaxPrice.Value)
                continue;

            int ruleScore = CatalogCategoryDefinitions.EvaluateMatchScore(
                p.Name,
                p.Description,
                p.TagNames,
                p.HasSuitableZodiacs,
                p.Price,
                p.OriginalPrice,
                rule);

            if (ruleScore <= 0)
                continue;

            // Nếu người dùng nhập thêm từ khóa tìm kiếm trên thanh Search
            if (!string.IsNullOrEmpty(cleanKeyword))
            {
                var nameLower = p.Name.ToLowerInvariant();
                var descLower = (p.Description ?? "").ToLowerInvariant();
                var tagJoined = string.Join(" ", p.TagNames).ToLowerInvariant();

                int kwScore = 0;
                if (nameLower.Contains(cleanKeyword)) kwScore += 150;
                if (tagJoined.Contains(cleanKeyword)) kwScore += 100;
                if (descLower.Contains(cleanKeyword)) kwScore += 50;

                if (kwScore == 0 && keywordTokens.Length > 0)
                {
                    int matchedTokens = keywordTokens.Count(t =>
                        CatalogCategoryDefinitions.ContainsKeywordBoundary(nameLower, t) ||
                        CatalogCategoryDefinitions.ContainsKeywordBoundary(tagJoined, t) ||
                        CatalogCategoryDefinitions.ContainsKeywordBoundary(descLower, t));

                    // Phải khớp ít nhất một nửa số từ khóa chính trở lên
                    int requiredTokens = Math.Max(1, (int)Math.Ceiling(keywordTokens.Length * 0.5));
                    if (matchedTokens >= requiredTokens)
                    {
                        kwScore += matchedTokens * 40;
                    }
                }

                if (kwScore == 0)
                    continue;

                ruleScore += kwScore;
            }

            scoredList.Add((p, ruleScore));
        }

        // 2. Sắp xếp kết quả
        bool IsActiveFeatured(CachedCatalogProduct p) =>
            p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value > now;

        double DiscountRatio(CachedCatalogProduct p) =>
            p.OriginalPrice.HasValue && p.OriginalPrice.Value > p.Price
                ? (double)(p.OriginalPrice.Value - p.Price) / p.OriginalPrice.Value
                : 0.0;

        var sortKey = (request.Sort ?? "relevance").Trim().ToLowerInvariant();

        IEnumerable<(CachedCatalogProduct Product, int Score)> ordered = sortKey switch
        {
            "price_asc" or "giá thấp đến cao" => scoredList
                .OrderBy(x => x.Product.Price)
                .ThenByDescending(x => x.Score),

            "price_desc" or "giá cao đến thấp" => scoredList
                .OrderByDescending(x => x.Product.Price)
                .ThenByDescending(x => x.Score),

            "rating" or "đánh giá cao" => scoredList
                .OrderByDescending(x => x.Product.Rating)
                .ThenByDescending(x => x.Product.TotalReviews)
                .ThenByDescending(x => x.Score),

            "popular" or "bán chạy nhất" => scoredList
                .OrderByDescending(x => x.Product.PopularityScore)
                .ThenByDescending(x => x.Product.TotalReviews)
                .ThenByDescending(x => x.Score),

            "discount" => scoredList
                .OrderByDescending(x => DiscountRatio(x.Product))
                .ThenByDescending(x => x.Score),

            _ => rule.OnlyDiscounted
                ? scoredList
                    .OrderByDescending(x => IsActiveFeatured(x.Product))
                    .ThenByDescending(x => DiscountRatio(x.Product))
                    .ThenByDescending(x => x.Product.PopularityScore)
                : scoredList
                    .OrderByDescending(x => IsActiveFeatured(x.Product))
                    .ThenByDescending(x => x.Score)
                    .ThenByDescending(x => x.Product.PopularityScore)
                    .ThenByDescending(x => x.Product.Rating)
                    .ThenByDescending(x => x.Product.TotalReviews)
        };

        // Nếu đang ở danh mục "tat-ca" hoặc "qua-tang-khong-can-dip" và sort mặc định -> trộn xen kẽ Shopee & TikTok Shop để giao diện đa dạng
        List<CachedCatalogProduct> finalOrderedProducts;
        if (rule.MatchAll && string.IsNullOrEmpty(cleanKeyword) && (sortKey == "relevance" || sortKey == "ai đề xuất tốt nhất" || sortKey == "đề xuất phù hợp nhất"))
        {
            var list = ordered.Select(x => x.Product).ToList();
            var shopee = list.Where(p => p.SourceEnum == ProductSource.Shopee).ToList();
            var tiktok = list.Where(p => p.SourceEnum == ProductSource.TikTokShop).ToList();
            var others = list.Where(p => p.SourceEnum != ProductSource.Shopee && p.SourceEnum != ProductSource.TikTokShop).ToList();

            finalOrderedProducts = new List<CachedCatalogProduct>(list.Count);
            int maxLen = Math.Max(shopee.Count, Math.Max(tiktok.Count, others.Count));
            for (int i = 0; i < maxLen; i++)
            {
                if (i < shopee.Count) finalOrderedProducts.Add(shopee[i]);
                if (i < tiktok.Count) finalOrderedProducts.Add(tiktok[i]);
                if (i < others.Count) finalOrderedProducts.Add(others[i]);
            }
        }
        else
        {
            finalOrderedProducts = ordered.Select(x => x.Product).ToList();
        }

        // 3. Phân trang thật phía Server
        int total = finalOrderedProducts.Count;
        int safePageSize = Math.Clamp(request.PageSize, 1, 60);
        int totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / safePageSize);
        int safePage = totalPages == 0 ? 1 : Math.Clamp(request.Page, 1, totalPages);

        var pagedItems = finalOrderedProducts
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(p => new CatalogProductItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                ImageUrl = p.ImageUrl,
                OriginalUrl = p.OriginalUrl,
                AffiliateUrl = p.AffiliateUrl,
                Source = p.Source,
                Rating = p.Rating,
                TotalReviews = p.TotalReviews,
                IsFeatured = IsActiveFeatured(p),
                Tags = p.TagNames
            })
            .ToList();

        var displayTitle = !string.IsNullOrEmpty(cleanKeyword)
            ? $"Kết quả tìm kiếm: \"{request.Keyword!.Trim()}\""
            : rule.Title;

        var displayGroup = !string.IsNullOrEmpty(cleanKeyword)
            ? "Tìm kiếm quà tặng"
            : rule.Group;

        return new CatalogProductsResultDto
        {
            Category = new CatalogCategoryInfoDto
            {
                Slug = rule.Slug,
                Title = displayTitle,
                Group = displayGroup
            },
            Total = total,
            Page = safePage,
            PageSize = safePageSize,
            TotalPages = totalPages,
            Items = pagedItems
        };
    }

    public async Task<List<CatalogCategoryCountDto>> GetCategoriesWithCountAsync(CancellationToken cancellationToken)
    {
        var allProducts = await GetApprovedTaggedProductsAsync(cancellationToken);
        var result = new List<CatalogCategoryCountDto>();

        foreach (var rule in CatalogCategoryDefinitions.All)
        {
            int count = 0;
            foreach (var p in allProducts)
            {
                int score = CatalogCategoryDefinitions.EvaluateMatchScore(
                    p.Name,
                    p.Description,
                    p.TagNames,
                    p.HasSuitableZodiacs,
                    p.Price,
                    p.OriginalPrice,
                    rule);
                if (score > 0) count++;
            }

            result.Add(new CatalogCategoryCountDto
            {
                Slug = rule.Slug,
                Title = rule.Title,
                Group = rule.Group,
                Count = count
            });
        }

        return result;
    }

    public async Task<List<CatalogProductItemDto>> GetRelatedProductsAsync(Guid productId, int limit = 10, CancellationToken cancellationToken = default)
    {
        var allProducts = await GetApprovedTaggedProductsAsync(cancellationToken);
        var current = allProducts.FirstOrDefault(p => p.Id == productId);

        if (current == null || current.TagNames == null || current.TagNames.Count == 0)
        {
            return new List<CatalogProductItemDto>();
        }

        var currentTags = new HashSet<string>(current.TagNames, StringComparer.OrdinalIgnoreCase);

        // Xác định các danh mục dòng sản phẩm / người nhận mà sản phẩm hiện tại thuộc về
        var currentMatchedCategories = CatalogCategoryDefinitions.All
            .Where(r => !r.MatchAll && !r.OnlyDiscounted && r.Slug != "qua-sinh-nhat" && r.Slug != "qua-cung-hoang-dao")
            .Where(r => CatalogCategoryDefinitions.EvaluateMatchScore(
                current.Name,
                current.Description,
                current.TagNames,
                current.HasSuitableZodiacs,
                current.Price,
                current.OriginalPrice,
                r) > 0)
            .Select(r => r.Slug)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        bool IsActiveFeatured(CachedCatalogProduct p) =>
            p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value > now;

        var candidates = new List<(CachedCatalogProduct Product, int Score)>();

        foreach (var p in allProducts)
        {
            if (p.Id == productId) continue;
            if (p.TagNames == null || p.TagNames.Count == 0) continue;

            int sharedTagCount = p.TagNames.Count(t => currentTags.Contains(t));
            if (sharedTagCount == 0) continue; // Tuyệt đối không nhét bừa sản phẩm không có chung Tag nào

            int score = sharedTagCount * 20;

            // Cộng điểm nếu cùng dòng sản phẩm / đối tượng người nhận cụ thể
            if (currentMatchedCategories.Count > 0)
            {
                int sharedCatCount = 0;
                foreach (var rule in CatalogCategoryDefinitions.All)
                {
                    if (!currentMatchedCategories.Contains(rule.Slug)) continue;
                    int catScore = CatalogCategoryDefinitions.EvaluateMatchScore(
                        p.Name, p.Description, p.TagNames, p.HasSuitableZodiacs, p.Price, p.OriginalPrice, rule);
                    if (catScore > 0) sharedCatCount++;
                }
                score += sharedCatCount * 25;
            }

            // Cộng điểm nếu mức giá tương đồng (±40% giá sản phẩm đang xem)
            if (current.Price > 0)
            {
                double diffRatio = Math.Abs(p.Price - current.Price) / (double)current.Price;
                if (diffRatio <= 0.4) score += 15;
                else if (diffRatio <= 0.75) score += 5;
            }

            candidates.Add((p, score));
        }

        int safeLimit = Math.Clamp(limit, 1, 20);

        return candidates
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => IsActiveFeatured(x.Product))
            .ThenByDescending(x => x.Product.Rating)
            .ThenByDescending(x => x.Product.TotalReviews)
            .Take(safeLimit)
            .Select(x => new CatalogProductItemDto
            {
                Id = x.Product.Id,
                Name = x.Product.Name,
                Description = x.Product.Description,
                Price = x.Product.Price,
                OriginalPrice = x.Product.OriginalPrice,
                ImageUrl = x.Product.ImageUrl,
                OriginalUrl = x.Product.OriginalUrl,
                AffiliateUrl = x.Product.AffiliateUrl,
                Source = x.Product.Source,
                Rating = x.Product.Rating,
                TotalReviews = x.Product.TotalReviews,
                IsFeatured = IsActiveFeatured(x.Product),
                Tags = x.Product.TagNames
            })
            .ToList();
    }
}

