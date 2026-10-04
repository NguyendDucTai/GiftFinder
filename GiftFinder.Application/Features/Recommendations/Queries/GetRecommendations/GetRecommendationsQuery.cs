using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class GetRecommendationsQuery : IRequest<List<ProductRecommendationDto>>
{
    public long? MaxBudget { get; set; }
    public ZodiacSign? TargetZodiac { get; set; }
    public Guid? OccasionTagId { get; set; }
    public Guid? InterestTagId { get; set; }
    
    // Thông tin phân loại và cá nhân hóa cho AI
    public int? TargetAge { get; set; } // Độ tuổi
    public string? Relationship { get; set; } // Ví dụ: "bạn gái", "mẹ", "đồng nghiệp"
}
