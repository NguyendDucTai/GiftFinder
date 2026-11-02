using System;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

/// <summary>
/// DTO chứa thông tin tóm lược của sản phẩm ứng viên gửi cho AI thẩm định
/// </summary>
public class CandidateProductItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public int PriceTier { get; set; } = 1; // 1 = Tầng chuẩn, 2 = Tầng nới lỏng
    public string? Category { get; set; }
    public double Rating { get; set; }
    public int TotalReviews { get; set; }
}

/// <summary>
/// Kết quả thẩm định, chọn lựa sản phẩm và viết lời tư vấn từ AI Reranker
/// </summary>
public class AiRerankResultDto
{
    public Guid? SelectedProductId { get; set; }
    public string? Advice { get; set; }
    public bool IsRelaxedBudget { get; set; }
    public string? SelectionReason { get; set; }
}

