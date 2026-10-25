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
            throw new InvalidOperationException("Sản phẩm này đã có trong danh sách theo dõi giá của bạn.");
        }

        // 2. Kiểm tra hạn mức theo gói cước (Freemium Quota & Premium Limit)
        int maxAllowed = user.Tier == MembershipTier.Premium ? 20 : 3;

        if (user.Tier == MembershipTier.Free)
        {
            // Đếm tổng số bản ghi đang có từ trước đến nay
            var totalCreatedCount = await _context.PriceTrackings
                .CountAsync(pt => pt.UserId == userId, cancellationToken);

            if (totalCreatedCount >= 3)
            {
                // Đã hết 3 lượt ưu đãi dùng thử -> Kiểm tra xem hôm nay đã thêm sản phẩm nào chưa
                var todayUtc = DateTime.UtcNow.Date;
                var addedTodayCount = await _context.PriceTrackings
                    .CountAsync(pt => pt.UserId == userId && pt.CreatedAt >= todayUtc, cancellationToken);

                if (addedTodayCount >= 1)
                {
                    throw new InvalidOperationException(
                        "Tài khoản Miễn phí chỉ được thêm 1 sản phẩm theo dõi mỗi ngày. " +
                        "Vui lòng quay lại vào ngày mai hoặc nâng cấp Premium để theo dõi không giới hạn ngay bây giờ.");
                }
            }
        }
        else // Premium User
        {
            var activeCount = await _context.PriceTrackings
                .CountAsync(pt => pt.UserId == userId && pt.IsActive, cancellationToken);

            if (activeCount >= 20)
            {
                throw new InvalidOperationException("Bạn đã đạt giới hạn 20 sản phẩm theo dõi giá, vui lòng bỏ theo dõi bớt sản phẩm khác.");
            }
        }

        Guid trackingId;

        // 3. Khôi phục hoặc tạo mới
        if (existingTracking != null)
        {
            existingTracking.Reactivate(product.Price);
            if (existingTracking.IsDeleted)
            {
                existingTracking.IsDeleted = false;
            }
            trackingId = existingTracking.Id;
        }
        else
        {
            var newTracking = new PriceTracking(userId, request.ProductId, product.Price);
            _context.PriceTrackings.Add(newTracking);
            trackingId = newTracking.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var currentActiveCount = await _context.PriceTrackings
            .CountAsync(pt => pt.UserId == userId && pt.IsActive, cancellationToken);

        return new TrackProductPriceResultDto
        {
            Id = trackingId,
            ProductId = product.Id,
            ProductName = product.Name,
            InitialPrice = product.Price,
            Message = "Bắt đầu theo dõi biến động giá sản phẩm thành công.",
            ActiveTrackedCount = currentActiveCount,
            MaxAllowed = maxAllowed
        };
    }
}
