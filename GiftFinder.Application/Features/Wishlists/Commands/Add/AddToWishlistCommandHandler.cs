using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Wishlists.Commands.Add;

public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddToWishlistCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để lưu sản phẩm vào danh sách yêu thích.");

        var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
            throw new ArgumentException("Sản phẩm không tồn tại.");

        var wishlistCount = await _context.Wishlists.CountAsync(w => w.UserId == userId, cancellationToken);
        
        var existingWishlistItem = await _context.Wishlists
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId, cancellationToken);

        if (existingWishlistItem != null)
        {
            if (!existingWishlistItem.IsDeleted)
            {
                throw new InvalidOperationException("Sản phẩm này đã có trong danh sách yêu thích của bạn.");
            }
            else
            {
                if (wishlistCount >= 100)
                    throw new InvalidOperationException("Danh sách yêu thích đã đạt giới hạn 100 sản phẩm, vui lòng xóa bớt trước khi thêm mới.");
                
                existingWishlistItem.Restore(request.TargetPrice, request.Note);
                await _context.SaveChangesAsync(cancellationToken);
                return existingWishlistItem.Id;
            }
        }

        if (wishlistCount >= 100)
            throw new InvalidOperationException("Danh sách yêu thích đã đạt giới hạn 100 sản phẩm, vui lòng xóa bớt trước khi thêm mới.");

        var wishlistItem = new Wishlist(userId, request.ProductId, request.TargetPrice, request.Note);
        _context.Wishlists.Add(wishlistItem);
        
        await _context.SaveChangesAsync(cancellationToken);

        return wishlistItem.Id;
    }
}
