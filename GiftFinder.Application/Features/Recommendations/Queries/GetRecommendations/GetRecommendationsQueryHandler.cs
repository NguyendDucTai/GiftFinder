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
        // 1. Lọc dữ liệu cơ bản (Ngân sách, Trạng thái)
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.Status == Domain.Enums.ProductStatus.Approved);

        if (request.MaxBudget.HasValue)
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

        // Tải danh sách về bộ nhớ để sắp xếp linh hoạt
        var products = await query.ToListAsync(cancellationToken);

        // 2. Tùy chọn sắp xếp ưu tiên (Boost) Cung Hoàng Đạo & Điểm Tương tác (PopularityScore)
        if (request.TargetZodiac.HasValue)
        {
            products = products
                .OrderByDescending(p => p.SuitableZodiacs != null && p.SuitableZodiacs.Contains(request.TargetZodiac.Value))
                .ThenByDescending(p => p.PopularityScore)
                .ThenByDescending(p => p.Rating)
                .ToList();
        }
        else
        {
            products = products
                .OrderByDescending(p => p.PopularityScore)
                .ThenByDescending(p => p.Rating)
                .ToList();
        }

        int totalCount = products.Count;

        // 3. Đóng gói kết quả (Phân trang)
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

        // 4. Nếu là trang đầu tiên và có chọn Cung Hoàng Đạo -> Gọi AI sinh lời khuyên cho Món Quà Top 1
        if (request.PageNumber == 1 && request.TargetZodiac.HasValue && pagedProducts.Any())
        {
            var topGift = pagedProducts.First();
            var zodiacName = GetZodiacName(request.TargetZodiac.Value);
            
            // Gọi API Gemini
            topGift.AiAdvice = await _aiService.GenerateGiftAdviceAsync(topGift.Name, zodiacName, request.Relationship, request.TargetAge);
        }

        // 5. Sinh bản ghi log tìm kiếm để phục vụ đánh giá ngầm (UC-04)
        var recommendationLog = new RecommendationLog(
            searchText: null,
            recipient: request.Relationship,
            occasion: request.OccasionTagId?.ToString(),
            budgetMin: null,
            budgetMax: request.MaxBudget,
            hobbies: request.InterestTagId?.ToString(),
            resultCount: totalCount
        );
        
        _context.RecommendationLogs.Add(recommendationLog);
        await _context.SaveChangesAsync(cancellationToken);

        return new PagedRecommendationResult
        {
            Items = pagedProducts,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            RecommendationLogId = recommendationLog.Id
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
