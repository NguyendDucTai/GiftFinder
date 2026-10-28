using GiftFinder.Application.Features.Affiliate.DTOs;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Application.Common.Interfaces;

public interface IAffiliateNetworkService
{
    /// <summary>
    /// Đồng bộ hàng loạt sản phẩm theo từ khóa từ sàn TMĐT (Shopee hoặc TikTok Shop qua Accesstrade)
    /// </summary>
    Task<List<AffiliateProductFeedItem>> FetchFeedAsync(
        string keyword, 
        ProductSource source, 
        int limit = 20, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bóc tách và lấy thông tin chi tiết của 1 sản phẩm cụ thể từ đường link Shopee hoặc TikTok Shop
    /// </summary>
    Task<AffiliateProductFeedItem?> FetchProductByUrlAsync(
        string url, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo đường dẫn Affiliate Deeplink kèm mã định danh SubId (để tracking đơn hàng đối soát hoa hồng)
    /// </summary>
    Task<string> GenerateDeeplinkAsync(
        string originalUrl, 
        string subId, 
        CancellationToken cancellationToken = default);
}
