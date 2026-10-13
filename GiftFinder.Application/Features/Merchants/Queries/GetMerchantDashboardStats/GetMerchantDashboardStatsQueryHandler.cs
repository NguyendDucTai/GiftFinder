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

        var clickCounts = await _context.ClickTrackings
            .AsNoTracking()
            .Where(c => productIds.Contains(c.ProductId))
            .GroupBy(c => c.ProductId)
            .Select(g => new { ProductId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProductId, x => x.Count, cancellationToken);

        var totalClicks = clickCounts.Values.Sum();

        var topProducts = products
            .Select(p => new MerchantTopProductDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Price = p.Price,
                ClickCount = clickCounts.TryGetValue(p.Id, out var count) ? count : 0,
                IsFeatured = p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value > now
            })
            .OrderByDescending(p => p.ClickCount)
            .ThenByDescending(p => p.Price)
            .Take(5)
            .ToList();

        return new MerchantDashboardStatsDto
        {
            ShopName = merchant.ShopName,
            IsVerified = merchant.IsVerified,
            TotalProducts = totalProducts,
            ApprovedProducts = approvedProducts,
            PendingProducts = pendingProducts,
            RejectedProducts = rejectedProducts,
            FeaturedProducts = featuredProducts,
            TotalClicks = totalClicks,
            TopProducts = topProducts
        };
    }
}
