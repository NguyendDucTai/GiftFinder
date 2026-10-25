using System;
using MediatR;

namespace GiftFinder.Application.Features.PriceTrackings.Queries.GetProductPriceHistory;

public class GetProductPriceHistoryQuery : IRequest<ProductPriceHistoryVm>
{
    public Guid ProductId { get; set; }

    public GetProductPriceHistoryQuery(Guid productId)
    {
        ProductId = productId;
    }

    public GetProductPriceHistoryQuery() { }
}
