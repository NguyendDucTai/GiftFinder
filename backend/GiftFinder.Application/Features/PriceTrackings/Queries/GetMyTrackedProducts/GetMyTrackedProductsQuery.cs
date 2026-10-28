using MediatR;

namespace GiftFinder.Application.Features.PriceTrackings.Queries.GetMyTrackedProducts;

public class GetMyTrackedProductsQuery : IRequest<TrackedProductListVm>
{
    public bool OnlyPriceDropped { get; set; } = false;
}
