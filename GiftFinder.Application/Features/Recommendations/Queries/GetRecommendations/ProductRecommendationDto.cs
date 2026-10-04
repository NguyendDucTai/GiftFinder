namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class ProductRecommendationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public long Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? AffiliateUrl { get; set; }
    public string OriginalUrl { get; set; } = default!;
    public double Rating { get; set; }
    public int TotalReviews { get; set; }
    
    // Lời khuyên cá nhân hóa từ AI (Gemini)
    public string? AiAdvice { get; set; }
}
