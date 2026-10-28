using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.CancelFeaturedPlacement;

public class CancelFeaturedPlacementCommandHandler : IRequestHandler<CancelFeaturedPlacementCommand, CancelFeaturedPlacementResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CancelFeaturedPlacementCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CancelFeaturedPlacementResponseDto> Handle(CancelFeaturedPlacementCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thao tác.");
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

        product.DeactivateFeatured();
        await _context.SaveChangesAsync(cancellationToken);

        return new CancelFeaturedPlacementResponseDto
        {
            ProductId = product.Id,
            IsFeatured = false,
            Message = "Đã hủy gói tài trợ nổi bật thành công."
        };
    }
}
