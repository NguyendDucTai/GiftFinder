namespace GiftFinder.Application.Features.Affiliate.DTOs;

public class PlatformRevenueDetailDto
{
    public string PlatformName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public long Gmv { get; set; }
    public long PendingCommission { get; set; }
    public long ApprovedCommission { get; set; }
    public long TotalCommission { get; set; }
}

public class AffiliateRevenueReportDto
{
    public int TotalClicks { get; set; }
    public int ConvertedClicks { get; set; }
    public decimal ConversionRate { get; set; } // % (vd 4.25%)
    public int TotalOrders { get; set; }
    public long TotalGmv { get; set; } // Tổng giá trị đơn hàng (VNĐ)
    public long PendingCommission { get; set; }
    public long ApprovedCommission { get; set; }
    public long RejectedCommission { get; set; }
    public long TotalCommission { get; set; }
    public List<PlatformRevenueDetailDto> PlatformBreakdown { get; set; } = new();
}
