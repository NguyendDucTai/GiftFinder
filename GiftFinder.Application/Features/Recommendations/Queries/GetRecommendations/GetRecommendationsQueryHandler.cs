using GiftFinder.Domain.Entities;
using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class GetRecommendationsQueryHandler : IRequestHandler<GetRecommendationsQuery, PagedRecommendationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAiRecommendationService _aiService;

    public GetRecommendationsQueryHandler(IApplicationDbContext context, IAiRecommendationService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<PagedRecommendationResult> Handle(GetRecommendationsQuery request, CancellationToken cancellationToken)
    {
        ParsedRecommendationIntent? parsedIntent = null;

        // 1. Nếu người dùng nhập câu Prompt tự nhiên -> Dùng Gemini AI bóc tách ý định
        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            parsedIntent = await _aiService.ParsePromptAsync(request.Prompt, cancellationToken);

            // AI Guardrail: Nếu câu hỏi không liên quan đến tìm quà (lạc đề, quấy rối, vô nghĩa)
            if (parsedIntent != null && !parsedIntent.IsValidGiftIntent)
            {
                return new PagedRecommendationResult
                {
                    Items = new List<ProductRecommendationDto>(),
                    TotalCount = 0,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    IsValidIntent = false,
                    Message = parsedIntent.AiMessage,
                    InterpretedIntent = parsedIntent
                };
            }
        }

        // 2. Gộp tiêu chí thông minh (Ưu tiên bộ lọc thủ công nếu có, nếu không thì lấy từ AI)
        var effectiveMinBudget = request.MinBudget ?? parsedIntent?.MinBudget;
        var effectiveMaxBudget = request.MaxBudget ?? parsedIntent?.MaxBudget;
        var budgetType = parsedIntent?.BudgetType;

        if (string.IsNullOrEmpty(budgetType))
        {
            if (effectiveMinBudget.HasValue && !effectiveMaxBudget.HasValue) budgetType = "Above";
            else if (!effectiveMinBudget.HasValue && effectiveMaxBudget.HasValue) budgetType = "Below";
            else if (effectiveMinBudget.HasValue && effectiveMaxBudget.HasValue) budgetType = "Range";
        }

        var effectiveZodiac = request.TargetZodiac ?? parsedIntent?.TargetZodiac;
        var effectiveRelationship = !string.IsNullOrWhiteSpace(request.Relationship) 
            ? request.Relationship 
            : parsedIntent?.Relationship;
        var effectiveAge = request.TargetAge ?? parsedIntent?.TargetAge;

        // 3. Truy vấn Database cơ bản (Chỉ lọc cứng nếu người dùng chủ động chọn trên giao diện)
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.Status == Domain.Enums.ProductStatus.Approved);

        if (request.MinBudget.HasValue && request.MinBudget.Value > 0)
        {
            query = query.Where(p => p.Price >= request.MinBudget.Value);
        }

        if (request.MaxBudget.HasValue && request.MaxBudget.Value > 0)
        {
            query = query.Where(p => p.Price <= request.MaxBudget.Value);
        }

        if (request.OccasionTagId.HasValue)
        {
            query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == request.OccasionTagId.Value));
        }

        if (request.InterestTagId.HasValue)
        {
            query = query.Where(p => p.ProductTags.Any(pt => pt.TagId == request.InterestTagId.Value));
        }

        var products = await query.ToListAsync(cancellationToken);

        // 4. Tính điểm phù hợp (Match Scoring) dựa trên từ khóa do AI bóc tách
        var interestKeywords = parsedIntent?.InterestKeywords ?? new List<string>();
        var occasionKeyword = parsedIntent?.Occasion;
        var relationshipKeyword = effectiveRelationship?.ToLowerInvariant();

        // Hàm tính điểm liên quan cho từng sản phẩm
        int CalculateKeywordMatchScore(Product p)
        {
            int score = 0;
            var nameLower = p.Name.ToLowerInvariant();
            var descLower = (p.Description ?? "").ToLowerInvariant();

            // Khớp Dịp tặng
            if (!string.IsNullOrEmpty(occasionKeyword))
            {
                var occLower = occasionKeyword.ToLowerInvariant();
                if (nameLower.Contains(occLower) || descLower.Contains(occLower) ||
                    p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(occLower)))
                {
                    score += 50;
                }
            }

            // Khớp Đối tượng tặng (Relationship)
            if (!string.IsNullOrEmpty(relationshipKeyword))
            {
                if (nameLower.Contains(relationshipKeyword) || descLower.Contains(relationshipKeyword) ||
                    p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(relationshipKeyword)))
                {
                    score += 40;
                }

                if (relationshipKeyword == "mẹ" || relationshipKeyword == "bố mẹ")
                {
                    if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("20/10") || pt.Tag.Name.Contains("8/3") || pt.Tag.Name.ToLowerInvariant().Contains("bố mẹ")))
                    {
                        score += 30;
                    }
                }
                else if (relationshipKeyword == "bạn gái" || relationshipKeyword == "người yêu" || relationshipKeyword == "crush")
                {
                    if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("Valentine") || pt.Tag.Name.Contains("20/10") || pt.Tag.Name.Contains("8/3")))
                    {
                        score += 30;
                    }
                }
            }

            // Khớp Sở thích / Ngành hàng
            foreach (var kw in interestKeywords)
            {
                var kwLower = kw.ToLowerInvariant();
                if (nameLower.Contains(kwLower) || descLower.Contains(kwLower) ||
                    p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kwLower)))
                {
                    score += 30;
                }
            }

            return score;
        }

        var now = DateTime.UtcNow;
        bool IsActiveFeatured(Product p) => p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value > now;

        // Hàm tính Tầng giá (Price Tier) theo đúng nguyên lý phân tầng:
        // Tier 1: Khớp chuẩn 100% khoảng giá khách muốn (trên 200k => >= 200k)
        // Tier 2: Hết Tier 1 thì hiển thị các món loanh quanh (tầm 100k - 1tr)
        // Tier 3: Các món còn lại
        int GetPriceTier(Product p)
        {
            if (!effectiveMinBudget.HasValue && !effectiveMaxBudget.HasValue)
                return 1;

            if (budgetType == "Above" && effectiveMinBudget.HasValue)
            {
                if (p.Price >= effectiveMinBudget.Value) return 1; // Khớp chuẩn >= 200k
                if (p.Price >= 100000 && p.Price < effectiveMinBudget.Value) return 2; // Loanh quanh cận dưới (100k - 200k)
                return 3; // Quá xa
            }

            if (budgetType == "Below" && effectiveMaxBudget.HasValue)
            {
                if (p.Price <= effectiveMaxBudget.Value) return 1; // Khớp chuẩn <= 200k
                if (p.Price > effectiveMaxBudget.Value && p.Price <= effectiveMaxBudget.Value * 1.5) return 2; // Loanh quanh cận trên
                return 3; // Quá xa
            }

            if ((budgetType == "Around" || budgetType == "Range") && (effectiveMinBudget.HasValue || effectiveMaxBudget.HasValue))
            {
                var min = effectiveMinBudget ?? 0;
                var max = effectiveMaxBudget ?? long.MaxValue;

                if (p.Price >= min && p.Price <= max) return 1; // Khớp chuẩn trong khoảng
                if (p.Price >= (min * 0.6) && p.Price <= (max * 1.5)) return 2; // Loanh quanh tầm giá
                return 3;
            }

            return 1;
        }

        // Hàm tính điểm cơ bản (Base Score) cho sản phẩm theo từ khóa, độ hot, rating và cung hoàng đạo
        double CalculateBaseScore(Product p)
        {
            double score = CalculateKeywordMatchScore(p);

            if (effectiveZodiac.HasValue && p.SuitableZodiacs != null && p.SuitableZodiacs.Contains(effectiveZodiac.Value))
            {
                score += 40;
            }

            score += (p.PopularityScore * 0.1);
            score += (p.Rating * 5);

            return score;
        }

        // BR-25: Điểm ưu tiên cho Featured (cộng tối đa 20% + 15 điểm ưu tiên)
        double CalculateFeaturedScore(Product p)
        {
            return (CalculateBaseScore(p) * 1.2) + 15;
        }

        // [BR-25: Relevance First] Kiểm tra sản phẩm Featured có đủ điều kiện lọt vào nhóm quảng cáo ưu tiên không
        bool IsEligibleFeatured(Product p)
        {
            if (!IsActiveFeatured(p)) return false;

            // Loại bỏ sản phẩm lệch tầng giá quá xa (Tier 3)
            if (GetPriceTier(p) > 2) return false;

            // Kiểm tra độ liên quan: Nếu người dùng có nhu cầu cụ thể (dịp, đối tượng, sở thích, cung hoàng đạo)
            // thì sản phẩm Featured bắt buộc phải có ít nhất 1 điểm liên quan thực tế
            bool hasSpecificIntent = !string.IsNullOrEmpty(occasionKeyword) ||
                                     !string.IsNullOrEmpty(relationshipKeyword) ||
                                     interestKeywords.Count > 0 ||
                                     effectiveZodiac.HasValue;

            if (hasSpecificIntent)
            {
                int keywordScore = CalculateKeywordMatchScore(p);
                bool matchesZodiac = effectiveZodiac.HasValue && p.SuitableZodiacs != null && p.SuitableZodiacs.Contains(effectiveZodiac.Value);

                if (keywordScore == 0 && !matchesZodiac)
                {
                    return false; // Lạc đề hoàn toàn -> không cho hiển thị trong slot tài trợ
                }
            }

            return true;
        }

        // 5. Thuật toán Xếp hạng & Xen kẽ Vị trí có điều kiện (Interleaved Slotting - Amazon & Shopee Pattern)
        // Tách các sản phẩm thành nhóm Featured đủ điều kiện và nhóm Organic
        var eligibleFeatured = products.Where(IsEligibleFeatured).ToList();
        var eligibleFeaturedIds = eligibleFeatured.Select(f => f.Id).ToHashSet();
        var organicCandidates = products.Where(p => !eligibleFeaturedIds.Contains(p.Id)).ToList();

        // [Quy tắc 4: Xoay vòng công bằng theo lượt khách]
        // Ngẫu nhiên có trọng số nhẹ (Jitter 0 -> 3.0 điểm) để các shop có điểm ngang nhau luân phiên xuất hiện ở các lượt tìm kiếm khác nhau
        double GetRotationScore(Product p)
        {
            return CalculateFeaturedScore(p) + (Random.Shared.NextDouble() * 3.0);
        }

        // [Quy tắc 2: Thực lực Vị trí #1]
        // Tìm ứng viên tốt nhất của từng nhóm (ưu tiên Tier 1 trước, sau đó đến điểm số)
        var bestFeatured = eligibleFeatured
            .OrderBy(p => GetPriceTier(p))
            .ThenByDescending(CalculateFeaturedScore)
            .ThenByDescending(p => p.PopularityScore)
            .FirstOrDefault();

        var bestOrganic = organicCandidates
            .OrderBy(p => GetPriceTier(p))
            .ThenByDescending(CalculateBaseScore)
            .ThenByDescending(p => p.PopularityScore)
            .FirstOrDefault();

        bool top1IsFeatured = false;
        Product? top1Product = null;

        if (bestFeatured != null && bestOrganic != null)
        {
            int featTier = GetPriceTier(bestFeatured);
            int orgTier = GetPriceTier(bestOrganic);

            if (featTier < orgTier)
            {
                top1IsFeatured = true;
                top1Product = bestFeatured;
            }
            else if (featTier > orgTier)
            {
                top1IsFeatured = false;
                top1Product = bestOrganic;
            }
            else
            {
                // Cùng tier -> so sánh điểm số thực tế
                if (CalculateFeaturedScore(bestFeatured) >= CalculateBaseScore(bestOrganic))
                {
                    top1IsFeatured = true;
                    top1Product = bestFeatured;
                }
                else
                {
                    top1IsFeatured = false;
                    top1Product = bestOrganic;
                }
            }
        }
        else if (bestFeatured != null)
        {
            top1IsFeatured = true;
            top1Product = bestFeatured;
        }
        else if (bestOrganic != null)
        {
            top1IsFeatured = false;
            top1Product = bestOrganic;
        }

        // Danh sách các ứng viên còn lại sau khi đã chốt Top 1
        var remainingFeatured = eligibleFeatured
            .Where(p => top1Product == null || p.Id != top1Product.Id)
            .OrderBy(p => GetPriceTier(p))
            .ThenByDescending(GetRotationScore)
            .ToList();

        var remainingOrganic = organicCandidates
            .Where(p => top1Product == null || p.Id != top1Product.Id)
            .OrderBy(p => GetPriceTier(p))
            .ThenByDescending(CalculateBaseScore)
            .ThenByDescending(p => p.CreatedAt)
            .ToList();

        // [Quy tắc 3: Lắp ráp danh sách theo công thức Xen kẽ]
        var rankedItems = new List<(Product Product, bool IsFeaturedSlot, string? BadgeLabel)>();

        if (top1Product != null)
        {
            rankedItems.Add((
                top1Product, 
                top1IsFeatured, 
                top1IsFeatured ? "Top Pick • Đối tác" : null
            ));

            int currentPos = 2;
            int featIndex = 0;
            int orgIndex = 0;

            bool IsTargetFeaturedSlot(int pos)
            {
                if (top1IsFeatured)
                {
                    // Top 1 đã là Featured -> các slot tiếp theo: Vị trí #5, #9 (giữa luôn có 3 organic)
                    if (pos == 5 || pos == 9) return true;
                    if (pos > 10 && (pos - 9) % 5 == 0) return true;
                }
                else
                {
                    // Top 1 là Organic -> các slot tiếp theo: Vị trí #3, #7 (giữa luôn có 3 organic)
                    if (pos == 3 || pos == 7) return true;
                    if (pos > 10 && (pos - 7) % 5 == 0) return true;
                }
                return false;
            }

            while (rankedItems.Count < products.Count)
            {
                bool wantFeatured = IsTargetFeaturedSlot(currentPos);

                if (wantFeatured && featIndex < remainingFeatured.Count)
                {
                    var fProd = remainingFeatured[featIndex++];
                    rankedItems.Add((fProd, true, "Được tài trợ"));
                }
                else if (orgIndex < remainingOrganic.Count)
                {
                    var oProd = remainingOrganic[orgIndex++];
                    rankedItems.Add((oProd, false, null));
                }
                else if (featIndex < remainingFeatured.Count)
                {
                    var fProd = remainingFeatured[featIndex++];
                    rankedItems.Add((fProd, true, "Được tài trợ"));
                }

                currentPos++;
            }
        }

        int totalCount = rankedItems.Count;

        // 6. Phân trang
        var pagedProducts = rankedItems
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new ProductRecommendationDto
            {
                Id = item.Product.Id,
                Name = item.Product.Name,
                Price = item.Product.Price,
                ImageUrl = item.Product.ImageUrl,
                AffiliateUrl = item.Product.AffiliateUrl,
                OriginalUrl = item.Product.OriginalUrl,
                Rating = item.Product.Rating,
                TotalReviews = item.Product.TotalReviews,
                IsFeatured = item.IsFeaturedSlot,
                BadgeLabel = item.BadgeLabel
            }).ToList();

        // 7. Gọi AI sinh lời khuyên cho Món Quà Top 1 ở trang đầu tiên
        if (request.PageNumber == 1 && pagedProducts.Any())
        {
            var topGift = pagedProducts.First();
            var zodiacName = effectiveZodiac.HasValue 
                ? GetZodiacName(effectiveZodiac.Value) 
                : null;
            var effectiveOccasion = parsedIntent?.Occasion;

            topGift.AiAdvice = await _aiService.GenerateGiftAdviceAsync(
                topGift.Name, 
                zodiacName, 
                effectiveRelationship, 
                effectiveAge,
                effectiveOccasion);
        }

        // 8. Sinh bản ghi log tìm kiếm để phục vụ đánh giá ngầm (UC-04)
        var recommendationLog = new RecommendationLog(
            searchText: request.Prompt,
            recipient: effectiveRelationship,
            occasion: request.OccasionTagId?.ToString() ?? parsedIntent?.Occasion,
            budgetMin: effectiveMinBudget,
            budgetMax: effectiveMaxBudget,
            hobbies: request.InterestTagId?.ToString() ?? string.Join(", ", interestKeywords),
            resultCount: totalCount
        );

        _context.RecommendationLogs.Add(recommendationLog);
        await _context.SaveChangesAsync(cancellationToken);

        // Chuẩn hóa tên cung hoàng đạo sang tiếng Việt trong Summary nếu AI lỡ viết tiếng Anh
        if (parsedIntent != null && effectiveZodiac.HasValue && !string.IsNullOrEmpty(parsedIntent.Summary))
        {
            var enZodiac = effectiveZodiac.Value.ToString();
            var vnZodiac = GetZodiacName(effectiveZodiac.Value);
            parsedIntent.Summary = parsedIntent.Summary.Replace(enZodiac, vnZodiac, StringComparison.OrdinalIgnoreCase);
        }

        return new PagedRecommendationResult
        {
            Items = pagedProducts,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            RecommendationLogId = recommendationLog.Id,
            InterpretedIntent = parsedIntent,
            IsValidIntent = true,
            Message = parsedIntent?.Summary
        };
    }

    private string GetZodiacName(Domain.Enums.ZodiacSign sign)
    {
        return sign switch
        {
            Domain.Enums.ZodiacSign.Aries => "Bạch Dương",
            Domain.Enums.ZodiacSign.Taurus => "Kim Ngưu",
            Domain.Enums.ZodiacSign.Gemini => "Song Tử",
            Domain.Enums.ZodiacSign.Cancer => "Cự Giải",
            Domain.Enums.ZodiacSign.Leo => "Sư Tử",
            Domain.Enums.ZodiacSign.Virgo => "Xử Nữ",
            Domain.Enums.ZodiacSign.Libra => "Thiên Bình",
            Domain.Enums.ZodiacSign.Scorpio => "Bọ Cạp",
            Domain.Enums.ZodiacSign.Sagittarius => "Nhân Mã",
            Domain.Enums.ZodiacSign.Capricorn => "Ma Kết",
            Domain.Enums.ZodiacSign.Aquarius => "Bảo Bình",
            Domain.Enums.ZodiacSign.Pisces => "Song Ngư",
            _ => sign.ToString()
        };
    }
}
