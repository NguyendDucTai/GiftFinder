using GiftFinder.Domain.Enums;

namespace GiftFinder.Application.Features.Affiliate.DTOs;

public class AffiliateProductFeedItem
{
    public string Name { get; set; } = string.Empty;
    public long Price { get; set; }
    public long? OriginalPrice { get; set; }
    public string? ImageUrl { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public string? AffiliateUrl { get; set; }
    public ProductSource Source { get; set; } = ProductSource.Shopee;
    public string? ExternalProductId { get; set; }
    public string? Description { get; set; }
    public double Rating { get; set; } = 4.8;
    public int TotalReviews { get; set; } = 120;
    public decimal CommissionRate { get; set; } = 0.08m; // Hoa hồng ước tính (vd 8%)
}
