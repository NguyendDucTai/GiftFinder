using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.PriceTrackings.Queries.GetMyTrackedProducts;

public class GetMyTrackedProductsQueryHandler : IRequestHandler<GetMyTrackedProductsQuery, TrackedProductListVm>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyTrackedProductsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TrackedProductListVm> Handle(GetMyTrackedProductsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem danh sách theo dõi giá.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Tài khoản người dùng không tồn tại.");

        // 1. Lấy danh sách sản phẩm đang theo dõi (IsActive == true)
        var query = _context.PriceTrackings
            .Include(pt => pt.Product)
            .Where(pt => pt.UserId == userId && pt.IsActive)
            .OrderByDescending(pt => pt.CreatedAt)
            .AsQueryable();

        var trackings = await query.ToListAsync(cancellationToken);

        var items = trackings.Select(pt => new TrackedProductDto
        {
            TrackingId = pt.Id,
            ProductId = pt.ProductId,
            ProductName = pt.Product.Name,
            ImageUrl = pt.Product.ImageUrl,
            Source = pt.Product.Source.ToString(),
            OriginalUrl = pt.Product.OriginalUrl,
            AffiliateUrl = pt.Product.AffiliateUrl,
            InitialPrice = pt.InitialPrice,
            CurrentPrice = pt.Product.Price,
            TrackedAt = pt.CreatedAt,
            LastNotifiedAt = pt.LastNotifiedAt
        }).ToList();

        if (request.OnlyPriceDropped)
        {
            items = items.Where(i => i.IsPriceDropped).ToList();
        }

        // 2. Tính toán hạn mức Quota
        var quota = new PriceTrackingQuotaDto
        {
            Tier = user.Tier.ToString(),
            ActiveCount = items.Count,
            MaxAllowed = user.Tier == MembershipTier.Premium ? 20 : 3
        };

        if (user.Tier == MembershipTier.Free)
        {
            var totalHistoricalCount = await _context.PriceTrackings
                .CountAsync(pt => pt.UserId == userId, cancellationToken);

            if (totalHistoricalCount < 3)
            {
                quota.CanAddToday = true;
                quota.RemainingDailyQuota = 3 - totalHistoricalCount;
                quota.QuotaDescription = $"Gói Miễn phí: Bạn đang trong lượt dùng thử ({totalHistoricalCount}/3 sản phẩm đã dùng).";
            }
            else
            {
                var todayUtc = DateTime.UtcNow.Date;
                var addedToday = await _context.PriceTrackings
                    .CountAsync(pt => pt.UserId == userId && pt.CreatedAt >= todayUtc, cancellationToken);

                quota.CanAddToday = addedToday == 0;
                quota.RemainingDailyQuota = Math.Max(0, 1 - addedToday);
                quota.QuotaDescription = quota.CanAddToday
                    ? "Gói Miễn phí: Hôm nay bạn có thể thêm 1 sản phẩm theo dõi."
                    : "Gói Miễn phí: Bạn đã sử dụng hết lượt thêm hôm nay (1 SP/ngày). Hãy quay lại vào ngày mai hoặc nâng cấp Premium.";
            }
        }
        else // Premium
        {
            quota.CanAddToday = quota.ActiveCount < 20;
            quota.RemainingDailyQuota = Math.Max(0, 20 - quota.ActiveCount);
            quota.QuotaDescription = $"Thành viên Premium ⭐: Bạn đang theo dõi {quota.ActiveCount}/20 sản phẩm.";
        }

        return new TrackedProductListVm
        {
            Quota = quota,
            Items = items
        };
    }
}
