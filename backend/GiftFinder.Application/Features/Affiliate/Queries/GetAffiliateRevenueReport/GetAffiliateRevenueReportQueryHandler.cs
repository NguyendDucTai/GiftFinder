using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Affiliate.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Affiliate.Queries.GetAffiliateRevenueReport;

public class GetAffiliateRevenueReportQueryHandler : IRequestHandler<GetAffiliateRevenueReportQuery, AffiliateRevenueReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetAffiliateRevenueReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AffiliateRevenueReportDto> Handle(GetAffiliateRevenueReportQuery request, CancellationToken cancellationToken)
    {
        // 1. Lọc thống kê Clicks
        var clicksQuery = _context.ClickTrackings.AsNoTracking();
        if (request.FromDate.HasValue)
            clicksQuery = clicksQuery.Where(c => c.ClickedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            clicksQuery = clicksQuery.Where(c => c.ClickedAt <= request.ToDate.Value);

        var totalClicks = await clicksQuery.CountAsync(cancellationToken);
        var convertedClicks = await clicksQuery.CountAsync(c => c.Status == ClickStatus.Converted, cancellationToken);

        // 2. Lọc thống kê Đơn hàng Affiliate
        var ordersQuery = _context.AffiliateOrders.AsNoTracking();
        if (request.FromDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.OrderedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.OrderedAt <= request.ToDate.Value);

        var orders = await ordersQuery.ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var totalGmv = orders.Sum(o => o.OrderAmount);
        var pendingCommission = orders.Where(o => o.Status == AffiliateOrderStatus.Pending).Sum(o => o.CommissionAmount);
        var approvedCommission = orders.Where(o => o.Status == AffiliateOrderStatus.Approved).Sum(o => o.CommissionAmount);
        var rejectedCommission = orders.Where(o => o.Status == AffiliateOrderStatus.Rejected).Sum(o => o.CommissionAmount);
        var totalCommission = pendingCommission + approvedCommission;

        var conversionRate = totalClicks > 0 
            ? Math.Round((decimal)convertedClicks * 100m / totalClicks, 2) 
            : 0m;

        // 3. Phân tách theo sàn TMĐT
        var platformBreakdown = new List<PlatformRevenueDetailDto>();
        var platforms = new[] 
        { 
            (Source: ProductSource.Shopee, Name: "Shopee Vietnam"),
            (Source: ProductSource.TikTokShop, Name: "TikTok Shop Vietnam"),
            (Source: ProductSource.Lazada, Name: "Lazada Vietnam"),
            (Source: ProductSource.Tiki, Name: "Tiki Vietnam")
        };

        foreach (var (platformSource, platformName) in platforms)
        {
            var platformOrders = orders.Where(o => o.Source == platformSource).ToList();

            var pending = platformOrders.Where(o => o.Status == AffiliateOrderStatus.Pending).Sum(o => o.CommissionAmount);
            var approved = platformOrders.Where(o => o.Status == AffiliateOrderStatus.Approved).Sum(o => o.CommissionAmount);

            platformBreakdown.Add(new PlatformRevenueDetailDto
            {
                PlatformName = platformName,
                OrderCount = platformOrders.Count,
                Gmv = platformOrders.Sum(o => o.OrderAmount),
                PendingCommission = pending,
                ApprovedCommission = approved,
                TotalCommission = pending + approved
            });
        }

        return new AffiliateRevenueReportDto
        {
            TotalClicks = totalClicks,
            ConvertedClicks = convertedClicks,
            ConversionRate = conversionRate,
            TotalOrders = totalOrders,
            TotalGmv = totalGmv,
            PendingCommission = pendingCommission,
            ApprovedCommission = approvedCommission,
            RejectedCommission = rejectedCommission,
            TotalCommission = totalCommission,
            PlatformBreakdown = platformBreakdown
        };
    }
}
