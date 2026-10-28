using System;
using MediatR;

namespace GiftFinder.Application.Features.PriceTrackings.Commands.UntrackProductPrice;

public class UntrackProductPriceCommand : IRequest<bool>
{
    public Guid ProductId { get; set; }

    public UntrackProductPriceCommand(Guid productId)
    {
        ProductId = productId;
    }

    public UntrackProductPriceCommand() { }
}
