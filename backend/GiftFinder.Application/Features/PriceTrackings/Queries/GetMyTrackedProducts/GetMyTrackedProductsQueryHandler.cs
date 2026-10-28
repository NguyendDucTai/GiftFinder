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

        // 1. Lấy danh sách sản phẩm theo dõi của user (bao gồm cả active và locked/expired để hiển thị khóa mờ)
        var query = _context.PriceTrackings
            .Include(pt => pt.Product)
            .Where(pt => pt.UserId == userId)
            .OrderByDescending(pt => pt.CreatedAt)
            .AsQueryable();

        var trackings = await query.ToListAsync(cancellationToken);

        // Kiểm tra và cập nhật tự động các món dùng thử đã quá 14 ngày
        var now = DateTime.UtcNow;
        bool stateChanged = false;
        foreach (var pt in trackings)
        {
            if (pt.IsActive && pt.ExpiresAt.HasValue && pt.ExpiresAt.Value <= now)
            {
                pt.ExpireTrial();
                stateChanged = true;
            }
        }

        if (stateChanged)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        var items = trackings.Select(pt =>
        {
            bool isLocked = pt.IsLocked || (pt.ExpiresAt.HasValue && pt.ExpiresAt.Value <= now);
            string? lockReason = pt.LockReason ?? (pt.ExpiresAt.HasValue && pt.ExpiresAt.Value <= now ? "TrialExpired" : null);
            int? daysRemaining = pt.ExpiresAt.HasValue 
                ? (int)Math.Max(0, Math.Ceiling((pt.ExpiresAt.Value - now).TotalDays)) 
                : null;

            return new TrackedProductDto
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
                LastNotifiedAt = pt.LastNotifiedAt,
                ExpiresAt = pt.ExpiresAt,
                IsLocked = isLocked,
                LockReason = lockReason,
                DaysRemaining = daysRemaining
            };
        }).ToList();

        if (request.OnlyPriceDropped)
        {
            items = items.Where(i => i.IsPriceDropped && !i.IsLocked).ToList();
        }

        // 2. Tính toán hạn mức Quota
        int activeCount = items.Count(i => !i.IsLocked);
        var quota = new PriceTrackingQuotaDto
        {
            Tier = user.Tier.ToString(),
            ActiveCount = activeCount,
            MaxAllowed = user.Tier == MembershipTier.Premium ? 20 : 3
        };

        if (user.Tier == MembershipTier.Free)
        {
            var totalHistoricalCount = await _context.PriceTrackings
                .CountAsync(pt => pt.UserId == userId, cancellationToken);

            quota.IsTrialExpired = totalHistoricalCount >= 3 || items.Any(i => i.IsLocked);
            quota.CanAddMore = totalHistoricalCount < 3;
            quota.UpgradePrompt = "Nâng cấp gói Premium (chỉ 29.000đ/tháng) để theo dõi 20 sản phẩm không giới hạn thời gian!";

            if (quota.CanAddMore)
            {
                quota.QuotaDescription = $"Gói Miễn phí: Bạn đang trong lượt dùng thử ({totalHistoricalCount}/3 sản phẩm đã dùng, hạn 14 ngày/sản phẩm).";
            }
            else
            {
                quota.QuotaDescription = "Gói Miễn phí: Bạn đã sử dụng hết 3 lượt dùng thử. Vui lòng nâng cấp Premium để tiếp tục theo dõi giá!";
            }
        }
        else // Premium
        {
            quota.IsTrialExpired = false;
            quota.CanAddMore = activeCount < 20;
            quota.QuotaDescription = $"Thành viên Premium ⭐: Bạn đang theo dõi {activeCount}/20 sản phẩm (Không giới hạn thời gian).";
            quota.UpgradePrompt = string.Empty;
        }

        return new TrackedProductListVm
        {
            Quota = quota,
            Items = items
        };
    }
}
