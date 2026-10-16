using System;
using System.Collections.Generic;

namespace GiftFinder.Application.Features.Products.Commands.SyncAllProductPrices;

public class SyncAllProductPricesResultDto
{
    public int TotalProducts { get; set; }
    public int TotalUpdated { get; set; }
    public int TotalUnchanged { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalFailed { get; set; }
    public double DurationSeconds { get; set; }
    public List<ProductSyncDetailDto> Details { get; set; } = new();
}

public class ProductSyncDetailDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public long OldPrice { get; set; }
    public long NewPrice { get; set; }
    public long PriceDifference => NewPrice - OldPrice;
    public string Status { get; set; } = string.Empty; // "Updated", "Unchanged", "Skipped", "Failed"
    public string? Message { get; set; }
    public string? ImageUrl { get; set; }
}
