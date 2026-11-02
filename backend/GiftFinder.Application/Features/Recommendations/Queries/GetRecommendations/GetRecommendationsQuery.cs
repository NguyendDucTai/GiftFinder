using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Recommendations.Queries.GetRecommendations;

public class GetRecommendationsQuery : IRequest<PagedRecommendationResult>
{
    // Tìm kiếm bằng câu tự nhiên (AI Prompt) - VD: "Tìm quà sinh nhật cho bạn gái thích decor tầm 500k"
    public string? Prompt { get; set; }

    public long? MinBudget { get; set; }
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

    // Danh sách ID các sản phẩm cần loại trừ (dạng chuỗi phân tách bởi dấu phẩy, dùng để tránh trùng món đã gợi ý trong phiên chat)
    public string? ExcludeProductIds { get; set; }

    // ID sản phẩm người dùng đang xem hoặc đang thắc mắc trong phiên chat (dùng cho Contextual Product Q&A)
    public Guid? CurrentProductId { get; set; }

    // Thông tin tóm tắt sản phẩm đang xem gửi từ client (nếu có)
    public string? CurrentProductContext { get; set; }

    public List<Guid> GetExcludedGuidList()
    {
        if (string.IsNullOrWhiteSpace(ExcludeProductIds)) return new();
        return ExcludeProductIds.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
    }
}
