using System;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice;

public class TrackProductPriceCommandHandler : IRequestHandler<TrackProductPriceCommand, TrackProductPriceResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TrackProductPriceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TrackProductPriceResultDto> Handle(TrackProductPriceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để theo dõi giá sản phẩm.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Tài khoản người dùng không tồn tại.");

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new ArgumentException("Sản phẩm không tồn tại.");

        // 1. Kiểm tra sản phẩm đã nằm trong danh sách theo dõi chưa
        var existingTracking = await _context.PriceTrackings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(pt => pt.UserId == userId && pt.ProductId == request.ProductId, cancellationToken);

        if (existingTracking != null && existingTracking.IsActive && !existingTracking.IsDeleted)
        {
            if (existingTracking.ExpiresAt.HasValue && existingTracking.ExpiresAt.Value <= DateTime.UtcNow)
            {
                existingTracking.ExpireTrial();
                await _context.SaveChangesAsync(cancellationToken);
                throw new InvalidOperationException(
                    "Sản phẩm này đã hết hạn dùng thử 14 ngày. Vui lòng nâng cấp gói Premium (chỉ 29.000đ/tháng) để mở khóa và tiếp tục theo dõi!");
            }

            throw new InvalidOperationException("Sản phẩm này đã có trong danh sách theo dõi giá của bạn.");
        }

        // 2. Kiểm tra hạn mức theo gói cước (Freemium 3 Items Trial & Premium 20 Items Limit)
        int maxAllowed = user.Tier == MembershipTier.Premium ? 20 : 3;
        DateTime? expiresAt = null;

        if (user.Tier == MembershipTier.Free)
        {
            // Đếm tổng số sản phẩm đã từng tạo theo dõi của người dùng Free
            var totalCreatedCount = await _context.PriceTrackings
                .CountAsync(pt => pt.UserId == userId, cancellationToken);

            if (totalCreatedCount >= 3)
            {
                throw new InvalidOperationException(
                    "Tài khoản Miễn phí chỉ được dùng thử tối đa 3 sản phẩm theo dõi giá. " +
                    "Vui lòng nâng cấp gói Premium (chỉ 29.000đ/tháng) để theo dõi 20 sản phẩm không giới hạn thời gian!");
            }

            // Gói Free có hạn dùng thử 14 ngày kể từ khi bấm theo dõi
            expiresAt = DateTime.UtcNow.AddDays(14);
        }
        else // Premium User
        {
            var activeCount = await _context.PriceTrackings
                .CountAsync(pt => pt.UserId == userId && pt.IsActive, cancellationToken);

            if (activeCount >= 20)
            {
                throw new InvalidOperationException("Bạn đã đạt giới hạn 20 sản phẩm theo dõi giá, vui lòng bỏ theo dõi bớt sản phẩm khác.");
            }

            // Gói Premium không giới hạn thời gian 14 ngày
            expiresAt = null;
        }

        Guid trackingId;

        // 3. Khôi phục hoặc tạo mới
        if (existingTracking != null)
        {
            existingTracking.Reactivate(product.Price, expiresAt);
            if (existingTracking.IsDeleted)
            {
                existingTracking.IsDeleted = false;
            }
            trackingId = existingTracking.Id;
        }
        else
        {
            var newTracking = new PriceTracking(userId, request.ProductId, product.Price, expiresAt);
            _context.PriceTrackings.Add(newTracking);
            trackingId = newTracking.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var currentActiveCount = await _context.PriceTrackings
            .CountAsync(pt => pt.UserId == userId && pt.IsActive, cancellationToken);

        int? daysRemaining = expiresAt.HasValue ? (int)Math.Max(0, Math.Ceiling((expiresAt.Value - DateTime.UtcNow).TotalDays)) : null;
        string successMessage = user.Tier == MembershipTier.Free
            ? "Bắt đầu theo dõi biến động giá thành công (Dùng thử miễn phí 14 ngày)."
            : "Bắt đầu theo dõi biến động giá thành công (Gói Premium không giới hạn thời gian).";

        return new TrackProductPriceResultDto
        {
            Id = trackingId,
            ProductId = product.Id,
            ProductName = product.Name,
            InitialPrice = product.Price,
            Message = successMessage,
            ActiveTrackedCount = currentActiveCount,
            MaxAllowed = maxAllowed,
            ExpiresAt = expiresAt,
            IsLocked = false,
            LockReason = null,
            DaysRemaining = daysRemaining
        };
    }
}
