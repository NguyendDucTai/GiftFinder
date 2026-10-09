namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class PagedRecommendationResult
{
    public List<ProductRecommendationDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    
    // ID dùng để tracking các tương tác ngầm (Click, Add to Wishlist) sau này
    public Guid RecommendationLogId { get; set; }

    // Ý định tìm kiếm do AI bóc tách (dùng để vẽ các chip hiển thị cho người dùng)
    public ParsedRecommendationIntent? InterpretedIntent { get; set; }

    // AI Guardrail: True nếu câu hỏi hợp lệ, False nếu lạc đề/quấy rối
    public bool IsValidIntent { get; set; } = true;
    public string? Message { get; set; }
}
