using System;

namespace GiftFinder.Application.Features.PriceTrackings.Queries.GetMyTrackedProducts;

public class TrackedProductDto
{
    public Guid TrackingId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? OriginalUrl { get; set; }
    public string? AffiliateUrl { get; set; }

    public long InitialPrice { get; set; }
    public long CurrentPrice { get; set; }
    public long PriceDifference => CurrentPrice - InitialPrice;
    public double PercentChange => InitialPrice > 0 ? Math.Round(((CurrentPrice - InitialPrice) / (double)InitialPrice) * 100, 1) : 0;
    public bool IsPriceDropped => (InitialPrice - CurrentPrice) >= (InitialPrice * 0.05);

    public DateTime TrackedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
}

public class PriceTrackingQuotaDto
{
    public string Tier { get; set; } = string.Empty;
    public int ActiveCount { get; set; }
    public int MaxAllowed { get; set; }
    public bool CanAddToday { get; set; }
    public int RemainingDailyQuota { get; set; }
    public string QuotaDescription { get; set; } = string.Empty;
}

public class TrackedProductListVm
{
    public PriceTrackingQuotaDto Quota { get; set; } = new();
    public List<TrackedProductDto> Items { get; set; } = new();
    public int TotalCount => Items.Count;
    public int DroppedCount => Items.Count(i => i.IsPriceDropped);
}
