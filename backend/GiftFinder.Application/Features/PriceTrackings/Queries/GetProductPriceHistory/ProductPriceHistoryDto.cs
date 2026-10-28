using System;

namespace GiftFinder.Application.Features.PriceTrackings.Queries.GetProductPriceHistory;

public class PriceHistoryPointDto
{
    public long OldPrice { get; set; }
    public long NewPrice { get; set; }
    public DateTime ChangedAt { get; set; }
    public long Difference => NewPrice - OldPrice;
    public double PercentChange => OldPrice > 0 ? Math.Round(((NewPrice - OldPrice) / (double)OldPrice) * 100, 1) : 0;
}

public class ProductPriceHistoryVm
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public long CurrentPrice { get; set; }
    public long LowestPrice { get; set; }
    public long HighestPrice { get; set; }
    public List<PriceHistoryPointDto> History { get; set; } = new();
}
