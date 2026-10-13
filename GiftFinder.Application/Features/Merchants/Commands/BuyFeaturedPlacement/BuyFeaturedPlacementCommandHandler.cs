using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.BuyFeaturedPlacement;

public class BuyFeaturedPlacementCommandHandler : IRequestHandler<BuyFeaturedPlacementCommand, BuyFeaturedPlacementResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public BuyFeaturedPlacementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BuyFeaturedPlacementResponseDto> Handle(BuyFeaturedPlacementCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để mua gói hiển thị nổi bật.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin sản phẩm.");
        }

        if (product.MerchantId != merchant.Id)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền thao tác trên sản phẩm này.");
        }

        if (product.Status != ProductStatus.Approved)
        {
            throw new InvalidOperationException("Sản phẩm chưa được duyệt, vui lòng chờ Admin xét duyệt trước khi mua gói hiển thị nổi bật.");
        }

        // Kích hoạt gói đẩy Top (Weekly: 7 ngày, Monthly: 1 tháng)
        product.ActivateFeatured(request.PlanType, DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);

        return new BuyFeaturedPlacementResponseDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            IsFeatured = product.IsFeatured,
            PlanType = request.PlanType.ToString(),
            FeaturedUntil = product.FeaturedUntil,
            Message = "Kích hoạt gói đẩy Top thành công! Sản phẩm sẽ được ưu tiên xuất hiện ở hàng đầu tiên trong danh sách kết quả gợi ý."
        };
    }
}
