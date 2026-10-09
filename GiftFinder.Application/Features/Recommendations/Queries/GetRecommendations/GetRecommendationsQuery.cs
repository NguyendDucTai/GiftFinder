using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class GetRecommendationsQuery : IRequest<PagedRecommendationResult>
{
    // Tìm kiếm bằng câu tự nhiên (AI Prompt) - VD: "Tìm quà sinh nhật cho bạn gái thích decor tầm 500k"
    public string? Prompt { get; set; }

    public long? MaxBudget { get; set; }
    public ZodiacSign? TargetZodiac { get; set; }
    public Guid? OccasionTagId { get; set; }
    public Guid? InterestTagId { get; set; }
    
    // Thông tin phân loại và cá nhân hóa cho AI
    public int? TargetAge { get; set; } // Độ tuổi
    public string? Relationship { get; set; } // Ví dụ: "bạn gái", "mẹ", "đồng nghiệp"
    
    // Phân trang
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
