using System;
using MediatR;

namespace GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice;

public class TrackProductPriceCommand : IRequest<TrackProductPriceResultDto>
{
    public Guid ProductId { get; set; }

    public TrackProductPriceCommand(Guid productId)
    {
        ProductId = productId;
    }

    public TrackProductPriceCommand() { }
}
