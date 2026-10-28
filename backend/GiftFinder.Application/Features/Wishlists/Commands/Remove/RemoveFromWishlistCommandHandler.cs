using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Wishlists.Commands.Remove;

public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFromWishlistCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");

        var wishlistItem = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId, cancellationToken);

        if (wishlistItem == null)
            throw new ArgumentException("Sản phẩm không có trong danh sách yêu thích của bạn.");

        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        wishlistItem.IsDeleted = true;
        wishlistItem.UpdatedAt = DateTime.UtcNow;

        if (product != null)
        {
            product.DecreasePopularityScore(8);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
