using GiftFinder.Application.Features.Wishlists.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.Wishlists.Queries.GetWishlist;

public record GetWishlistQuery() : IRequest<List<WishlistItemDto>>;
