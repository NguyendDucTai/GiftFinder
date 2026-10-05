using MediatR;

namespace GiftFinder.Application.Features.Wishlists.Commands.Add;

public record AddToWishlistCommand(Guid ProductId, long? TargetPrice = null, string? Note = null, Guid? RecommendationLogId = null) : IRequest<Guid>;
