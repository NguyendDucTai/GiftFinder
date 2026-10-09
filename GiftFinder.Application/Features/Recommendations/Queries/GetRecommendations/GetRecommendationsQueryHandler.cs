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
        var effectiveBudget = request.MaxBudget ?? parsedIntent?.MaxBudget;
        var effectiveZodiac = request.TargetZodiac ?? parsedIntent?.TargetZodiac;
        var effectiveRelationship = !string.IsNullOrWhiteSpace(request.Relationship) 
            ? request.Relationship 
            : parsedIntent?.Relationship;
        var effectiveAge = request.TargetAge ?? parsedIntent?.TargetAge;

        // 3. Truy vấn Database cơ bản (Ngân sách & Trạng thái duyệt)
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .Where(p => p.Status == Domain.Enums.ProductStatus.Approved);

        if (effectiveBudget.HasValue && effectiveBudget.Value > 0)
        {
            query = query.Where(p => p.Price <= effectiveBudget.Value);
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

        // 5. Xếp hạng sản phẩm ưu tiên: Khớp từ khóa AI -> Cung hoàng đạo -> Điểm hot (PopularityScore) -> Rating
        if (effectiveZodiac.HasValue)
        {
            products = products
                .OrderByDescending(p => CalculateKeywordMatchScore(p))
                .ThenByDescending(p => p.SuitableZodiacs != null && p.SuitableZodiacs.Contains(effectiveZodiac.Value))
                .ThenByDescending(p => p.PopularityScore)
                .ThenByDescending(p => p.Rating)
                .ToList();
        }
        else
        {
            products = products
                .OrderByDescending(p => CalculateKeywordMatchScore(p))
                .ThenByDescending(p => p.PopularityScore)
                .ThenByDescending(p => p.Rating)
                .ToList();
        }

        int totalCount = products.Count;

        // 6. Phân trang
        var pagedProducts = products
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductRecommendationDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                AffiliateUrl = p.AffiliateUrl,
                OriginalUrl = p.OriginalUrl,
                Rating = p.Rating,
                TotalReviews = p.TotalReviews
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
            budgetMin: null,
            budgetMax: effectiveBudget,
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
