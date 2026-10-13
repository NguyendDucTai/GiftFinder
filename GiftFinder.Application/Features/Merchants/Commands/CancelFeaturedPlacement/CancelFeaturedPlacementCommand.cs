using GiftFinder.Application.Features.Merchants.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.Merchants.Commands.CancelFeaturedPlacement;

public class CancelFeaturedPlacementCommand : IRequest<CancelFeaturedPlacementResponseDto>
{
    public Guid ProductId { get; set; }

    public CancelFeaturedPlacementCommand(Guid productId)
    {
        ProductId = productId;
    }
}
