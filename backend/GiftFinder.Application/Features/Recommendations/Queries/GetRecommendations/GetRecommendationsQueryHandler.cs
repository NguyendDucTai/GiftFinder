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

        // Nhận diện giới tính người nhận (Male / Female / Unisex)
        var effectiveGender = parsedIntent?.TargetGender;
        var promptText = (request.Prompt ?? "").ToLowerInvariant();
        var relLower = (effectiveRelationship ?? "").ToLowerInvariant();

        if (string.IsNullOrEmpty(effectiveGender))
        {
            if (promptText.Contains(" nam") || promptText.Contains("bạn trai") || promptText.Contains("chàng") || 
                promptText.Contains("bố") || promptText.Contains("cha") || promptText.Contains("anh trai") || 
                promptText.Contains("chồng") || promptText.Contains("ông nội") || promptText.Contains("ông ngoại") ||
                relLower.Contains("nam") || relLower.Contains("bạn trai"))
            {
                effectiveGender = "Male";
            }
            else if (promptText.Contains(" nữ") || promptText.Contains("bạn gái") || promptText.Contains("nàng") || 
                     promptText.Contains("mẹ") || promptText.Contains("má") || promptText.Contains("chị gái") || 
                     promptText.Contains("em gái") || promptText.Contains("vợ") || promptText.Contains("bà nội") ||
                     relLower.Contains("nữ") || relLower.Contains("bạn gái"))
            {
                effectiveGender = "Female";
            }
        }

        // Đồng bộ chi tiết quan hệ nếu giới tính đã rõ
        if (effectiveGender == "Male")
        {
            if (effectiveRelationship == "đồng nghiệp" || promptText.Contains("đồng nghiệp"))
                effectiveRelationship = "đồng nghiệp nam";
            else if (string.IsNullOrEmpty(effectiveRelationship))
            {
                if (promptText.Contains("bạn trai")) effectiveRelationship = "bạn trai";
                else if (promptText.Contains("sếp")) effectiveRelationship = "sếp nam";
                else if (promptText.Contains("bố") || promptText.Contains("cha") || promptText.Contains("ba")) effectiveRelationship = "bố";
                else if (promptText.Contains("anh trai")) effectiveRelationship = "anh trai";
                else if (promptText.Contains("chồng")) effectiveRelationship = "chồng";
            }
        }
        else if (effectiveGender == "Female")
        {
            if (effectiveRelationship == "đồng nghiệp" || promptText.Contains("đồng nghiệp"))
                effectiveRelationship = "đồng nghiệp nữ";
            else if (string.IsNullOrEmpty(effectiveRelationship))
            {
                if (promptText.Contains("bạn gái")) effectiveRelationship = "bạn gái";
                else if (promptText.Contains("sếp")) effectiveRelationship = "sếp nữ";
                else if (promptText.Contains("mẹ") || promptText.Contains("má")) effectiveRelationship = "mẹ";
                else if (promptText.Contains("chị gái")) effectiveRelationship = "chị gái";
                else if (promptText.Contains("em gái")) effectiveRelationship = "em gái";
                else if (promptText.Contains("vợ")) effectiveRelationship = "vợ";
            }
        }

        if (parsedIntent != null)
        {
            parsedIntent.TargetGender = effectiveGender;
            parsedIntent.Relationship = effectiveRelationship;
        }

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

        // Danh sách từ khóa nhận diện sản phẩm đặc thù theo giới tính
        var femaleProductIndicators = new[] { 
            "túi xách nữ", "nữ đeo chéo", "váy", "đầm", "son môi", "son dưỡng", 
            "nội y nữ", "chân váy", "băng vệ sinh", "dành cho nữ", "quà tặng bạn gái", 
            "dành cho bạn gái", "set quà bạn gái", "cho nàng", "nước hoa nữ" 
        };
        var maleProductIndicators = new[] { 
            "thắt lưng nam", "ví nam", "ví da nam", "dây nịt nam", "cà vạt", "nước hoa nam", 
            "dao cạo", "công nghệ", "bàn phím cơ", "chuột gaming", "tai nghe", "dành cho nam", 
            "quà tặng bạn trai", "cho chàng", "sáp vuốt tóc", "ví nịt nam", "ví thắt lưng nam" 
        };

        // Hàm tính điểm liên quan cho từng sản phẩm
        int CalculateKeywordMatchScore(Product p)
        {
            int score = 0;
            var nameLower = p.Name.ToLowerInvariant();
            var descLower = (p.Description ?? "").ToLowerInvariant();

            // Kiểm tra tính tương thích giới tính
            if (effectiveGender == "Male")
            {
                // Nếu tìm quà cho nam mà sản phẩm là đồ phụ nữ đặc trưng -> LOẠI THẲNG TAY
                if (femaleProductIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    return -9999;
                }

                if (maleProductIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    score += 150;
                }

                if (nameLower.Contains("nam") || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains("nam")))
                {
                    score += 80;
                }
            }
            else if (effectiveGender == "Female")
            {
                // Nếu tìm quà cho nữ mà sản phẩm là đồ nam giới đặc trưng -> LOẠI THẲNG TAY
                if (maleProductIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    return -9999;
                }

                if (femaleProductIndicators.Any(kw => nameLower.Contains(kw) || descLower.Contains(kw) || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains(kw))))
                {
                    score += 150;
                }

                if (nameLower.Contains("nữ") || p.ProductTags.Any(pt => pt.Tag.Name.ToLowerInvariant().Contains("nữ")))
                {
                    score += 80;
                }
            }

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

                if (relationshipKeyword.Contains("mẹ") || relationshipKeyword.Contains("bố mẹ"))
                {
                    if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("20/10") || pt.Tag.Name.Contains("8/3") || pt.Tag.Name.ToLowerInvariant().Contains("bố mẹ")))
                    {
                        score += 30;
                    }
                }
                else if (relationshipKeyword.Contains("bạn gái") || relationshipKeyword.Contains("người yêu") || relationshipKeyword.Contains("crush"))
                {
                    if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("Valentine") || pt.Tag.Name.Contains("20/10") || pt.Tag.Name.Contains("8/3")))
                    {
                        score += 30;
                    }
                }
                else if (relationshipKeyword.Contains("bạn trai") || relationshipKeyword.Contains("nam"))
                {
                    if (p.ProductTags.Any(pt => pt.Tag.Name.Contains("Valentine") || pt.Tag.Name.ToLowerInvariant().Contains("nam")))
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

        // Lọc bỏ triệt để các sản phẩm bị xung đột giới tính (score <= -1000)
        products = products.Where(p => CalculateKeywordMatchScore(p) > -1000).ToList();

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

                if (p.Price >= (min * 0.65) && p.Price <= (max * 1.3)) return 1; // Khớp chuẩn trong khoảng dung sai thực tế
                if (p.Price >= (min * 0.4) && p.Price <= (max * 1.7)) return 2; // Loanh quanh tầm giá
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
