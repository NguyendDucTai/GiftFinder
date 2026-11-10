using GiftFinder.Application.Common.Helpers;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Queries.GetMerchantDashboardStats;

public class GetMerchantDashboardStatsQueryHandler : IRequestHandler<GetMerchantDashboardStatsQuery, MerchantDashboardStatsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMerchantDashboardStatsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MerchantDashboardStatsDto> Handle(GetMerchantDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem thông tin gian hàng.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var now = DateTime.UtcNow;
        var safeDays = request.Days <= 7 ? 7 : 30;
        var fromDateUtc = now.Date.AddDays(-(safeDays - 1));

        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.MerchantId == merchant.Id)
            .ToListAsync(cancellationToken);

        var totalProducts = products.Count;
        var approvedProducts = products.Count(p => p.Status == ProductStatus.Approved);
        var pendingProducts = products.Count(p => p.Status == ProductStatus.Pending);
        var rejectedProducts = products.Count(p => p.Status == ProductStatus.Rejected);
        var featuredProducts = products.Count(p => p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value > now);

        var productIds = products.Select(p => p.Id).ToList();

        var allClicks = productIds.Count > 0
            ? await _context.ClickTrackings
                .AsNoTracking()
                .Where(c => productIds.Contains(c.ProductId))
                .Select(c => new { c.ProductId, c.ClickedAt })
                .ToListAsync(cancellationToken)
            : new();

        var clickCountsByProduct = allClicks
            .GroupBy(c => c.ProductId)
            .ToDictionary(g => g.Key, g => g.Count());

        var totalClicks = allClicks.Count;

        // Lấy toàn bộ đơn hàng của Shop
        var orders = await _context.MerchantOrders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.MerchantId == merchant.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count(o => o.Status != MerchantOrderStatus.Cancelled);
        var pendingOrders = orders.Count(o => o.Status == MerchantOrderStatus.Pending || o.Status == MerchantOrderStatus.Packing);
        var shippingOrders = orders.Count(o => o.Status == MerchantOrderStatus.Shipping);
        var completedOrders = orders.Count(o => o.Status == MerchantOrderStatus.Completed);
        var cancelledOrders = orders.Count(o => o.Status == MerchantOrderStatus.Cancelled);

        var completedRevenue = orders
            .Where(o => o.Status == MerchantOrderStatus.Completed)
            .Sum(o => o.TotalAmount);

        var pendingRevenue = orders
            .Where(o => o.Status == MerchantOrderStatus.Pending || o.Status == MerchantOrderStatus.Packing || o.Status == MerchantOrderStatus.Shipping)
            .Sum(o => o.TotalAmount);

        var averageOrderValue = totalOrders > 0
            ? (long)Math.Round(orders.Where(o => o.Status != MerchantOrderStatus.Cancelled).Average(o => (double)o.TotalAmount))
            : 0;

        var conversionRate = totalClicks > 0
            ? Math.Round((double)totalOrders / totalClicks * 100.0, 2)
            : (totalOrders > 0 ? 100.0 : 0.0);

        // Dữ liệu Biểu đồ tăng trưởng theo ngày (DailyChart)
        var dailyChart = new List<MerchantDailyChartPointDto>();
        for (int i = 0; i < safeDays; i++)
        {
            var dayStart = fromDateUtc.AddDays(i);
            var dayEnd = dayStart.AddDays(1);

            var dayClicks = allClicks.Count(c => c.ClickedAt >= dayStart && c.ClickedAt < dayEnd);
            var dayOrdersList = orders
                .Where(o => o.Status != MerchantOrderStatus.Cancelled && o.CreatedAt >= dayStart && o.CreatedAt < dayEnd)
                .ToList();

            dailyChart.Add(new MerchantDailyChartPointDto
            {
                DateLabel = dayStart.ToString("dd/MM"),
                FullDate = dayStart.ToString("yyyy-MM-dd"),
                Clicks = dayClicks,
                Orders = dayOrdersList.Count,
                Revenue = dayOrdersList.Sum(o => o.TotalAmount)
            });
        }

        // Thống kê số lượng bán & doanh thu theo từng sản phẩm
        var validOrderItems = orders
            .Where(o => o.Status != MerchantOrderStatus.Cancelled)
            .SelectMany(o => o.Items)
            .ToList();

        var soldByProduct = validOrderItems
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => (Qty: g.Sum(x => x.Quantity), Rev: g.Sum(x => x.LineTotal)));

        var topProducts = products
            .Select(p =>
            {
                var soldInfo = soldByProduct.TryGetValue(p.Id, out var s) ? s : (Qty: 0, Rev: 0L);
                return new MerchantTopProductDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    Price = p.Price,
                    ClickCount = clickCountsByProduct.TryGetValue(p.Id, out var count) ? count : 0,
                    SoldCount = soldInfo.Qty,
                    Revenue = soldInfo.Rev,
                    IsFeatured = p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value > now
                };
            })
            .OrderByDescending(p => p.SoldCount)
            .ThenByDescending(p => p.ClickCount)
            .ThenByDescending(p => p.Price)
            .Take(5)
            .ToList();

        var recentOrders = orders
            .Take(6)
            .Select(o => new MerchantOrderDto
            {
                Id = o.Id,
                OrderCode = o.OrderCode,
                MerchantId = o.MerchantId,
                ShopName = merchant.ShopName,
                ShopPickupProvince = merchant.PickupProvince,
                ReceiverName = o.ReceiverName,
                ReceiverPhone = o.ReceiverPhone,
                ReceiverProvince = o.ReceiverProvince,
                ReceiverDistrict = o.ReceiverDistrict,
                ReceiverAddress = o.ReceiverAddress,
                DeliveryNote = o.DeliveryNote,
                HasGiftWrap = o.HasGiftWrap,
                GiftMessage = o.GiftMessage,
                HidePriceOnPackage = o.HidePriceOnPackage,
                Subtotal = o.Subtotal,
                ShippingFee = o.ShippingFee,
                GiftWrapFee = o.GiftWrapFee,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.PaymentMethod,
                ShippingMethod = o.ShippingMethod.ToString(),
                ShippingCarrier = o.ShippingCarrier,
                TrackingCode = o.TrackingCode,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt,
                PackedAt = o.PackedAt,
                ShippedAt = o.ShippedAt,
                CompletedAt = o.CompletedAt,
                CancelReason = o.CancelReason,
                Items = o.Items.Select(i => new MerchantOrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImageUrl = i.ProductImageUrl,
                    Variant = i.Variant,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    LineTotal = i.LineTotal
                }).ToList()
            })
            .ToList();

        return new MerchantDashboardStatsDto
        {
            MerchantId = merchant.Id,
            ShopName = merchant.ShopName,
            ShopSlug = SlugHelper.ToUrlFriendlySlug(merchant.ShopName),
            PickupProvince = merchant.PickupProvince,
            PickupAddress = merchant.PickupAddress,
            IsVerified = merchant.IsVerified,
            TotalProducts = totalProducts,
            ApprovedProducts = approvedProducts,
            PendingProducts = pendingProducts,
            RejectedProducts = rejectedProducts,
            FeaturedProducts = featuredProducts,
            TotalClicks = totalClicks,
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            ShippingOrders = shippingOrders,
            CompletedOrders = completedOrders,
            CancelledOrders = cancelledOrders,
            CompletedRevenue = completedRevenue,
            PendingRevenue = pendingRevenue,
            AverageOrderValue = averageOrderValue,
            ConversionRate = conversionRate,
            DailyChart = dailyChart,
            RecentOrders = recentOrders,
            TopProducts = topProducts
        };
    }
}
