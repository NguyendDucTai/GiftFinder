using MediatR;

namespace GiftFinder.Application.Features.Wishlists.Commands.Remove;

public record RemoveFromWishlistCommand(Guid ProductId) : IRequest;
