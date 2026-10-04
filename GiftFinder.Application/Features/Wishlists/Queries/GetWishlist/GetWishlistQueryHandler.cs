using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Wishlists.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Wishlists.Queries.GetWishlist;

public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, List<WishlistItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetWishlistQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<WishlistItemDto>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");

        var wishlists = await _context.Wishlists
            .AsNoTracking()
            .Include(w => w.Product)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = wishlists.Select(w => new WishlistItemDto
        {
            Id = w.Id,
            ProductId = w.ProductId,
            ProductName = w.Product.Name,
            ImageUrl = w.Product.ImageUrl,
            Price = w.Product.Price,
            OriginalPrice = w.Product.OriginalPrice,
            OriginalUrl = w.Product.OriginalUrl,
            AffiliateUrl = w.Product.AffiliateUrl,
            TargetPrice = w.TargetPrice,
            Note = w.Note,
            AddedAt = w.CreatedAt,
            IsAvailable = w.Product.Status == ProductStatus.Approved,
            AvailabilityMessage = w.Product.Status != ProductStatus.Approved ? "Sản phẩm hiện không còn khả dụng." : null
        }).ToList();

        return result;
    }
}
