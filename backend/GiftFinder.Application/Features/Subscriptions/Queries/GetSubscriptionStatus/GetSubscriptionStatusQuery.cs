using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Subscriptions.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Subscriptions.Queries.GetSubscriptionStatus;

public class GetSubscriptionStatusQuery : IRequest<SubscriptionStatusDto>
{
}

public class GetSubscriptionStatusQueryHandler : IRequestHandler<GetSubscriptionStatusQuery, SubscriptionStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetSubscriptionStatusQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SubscriptionStatusDto> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản.");
        }

        var now = DateTime.UtcNow;
        bool isPremiumActive = user.Tier == MembershipTier.Premium && 
            ((user.PremiumExpiresAt.HasValue && user.PremiumExpiresAt.Value > now) || 
             (user.GracePeriodUntil.HasValue && user.GracePeriodUntil.Value > now));

        bool isInGracePeriod = user.GracePeriodUntil.HasValue && user.GracePeriodUntil.Value > now;

        int daysRemaining = 0;
        if (user.PremiumExpiresAt.HasValue && user.PremiumExpiresAt.Value > now)
        {
            daysRemaining = (int)Math.Ceiling((user.PremiumExpiresAt.Value - now).TotalDays);
        }

        var benefits = new List<string>
        {
            "Nhắc lịch tự động không giới hạn (tối đa 50 sự kiện/năm kèm email gửi hoa chúc & gợi ý quà)",
            "AI tư vấn cá nhân hóa chuyên sâu theo Cung hoàng đạo và thói quen sở thích",
            "Đặc quyền theo dõi biến động giá quà tặng (Price Tracking)"
        };

        return new SubscriptionStatusDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Tier = user.Tier.ToString(),
            IsPremiumActive = isPremiumActive,
            PremiumExpiresAt = user.PremiumExpiresAt,
            DaysRemaining = daysRemaining,
            IsInGracePeriod = isInGracePeriod,
            GracePeriodUntil = user.GracePeriodUntil,
            AutoRenew = user.AutoRenew,
            Benefits = benefits
        };
    }
}
