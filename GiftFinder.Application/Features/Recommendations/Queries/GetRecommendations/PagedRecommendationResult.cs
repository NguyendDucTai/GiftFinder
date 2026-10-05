namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class PagedRecommendationResult
{
    public List<ProductRecommendationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    
    // ID dùng để tracking các tương tác ngầm (Click, Add to Wishlist) sau này
    public Guid RecommendationLogId { get; set; }
}
