using System;

namespace GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice;

public class TrackProductPriceResultDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public long InitialPrice { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ActiveTrackedCount { get; set; }
    public int MaxAllowed { get; set; }
}
