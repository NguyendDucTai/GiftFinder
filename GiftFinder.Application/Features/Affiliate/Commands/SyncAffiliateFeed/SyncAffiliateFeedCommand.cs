using GiftFinder.Domain.Enums;
using MediatR;

namespace GiftFinder.Application.Features.Affiliate.Commands.SyncAffiliateFeed;

public class SyncAffiliateFeedCommand : IRequest<SyncAffiliateFeedResultDto>
{
    /// <summary>
    /// Từ khóa tìm kiếm quà tặng (Ví dụ: "quà sinh nhật", "nến thơm decor", "tai nghe chống ồn", "đồng hồ nữ")
    /// </summary>
    public string Keyword { get; set; } = "quà sinh nhật";

    /// <summary>
    /// Nguồn sàn TMĐT: Shopee (0) hoặc TikTokShop (1)
    /// </summary>
    public ProductSource Source { get; set; } = ProductSource.Shopee;

    /// <summary>
    /// Số lượng sản phẩm muốn kéo về (Mặc định 10)
    /// </summary>
    public int Limit { get; set; } = 10;

    /// <summary>
    /// Tự động chạy Groq AI giám định chất lượng quà và tự động gắn Tag Dịp, Sở thích, Cung hoàng đạo
    /// </summary>
    public bool AutoTagWithAi { get; set; } = true;
}

public class SyncAffiliateFeedResultDto
{
    public int TotalFetched { get; set; }
    public int TotalImported { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalRejectedByAi { get; set; }
    public List<string> ImportedProductNames { get; set; } = new();
    public List<string> RejectedDetails { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
