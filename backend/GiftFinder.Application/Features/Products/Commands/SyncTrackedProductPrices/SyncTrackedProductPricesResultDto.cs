using System;
using System.Collections.Generic;
using GiftFinder.Application.Features.Products.Commands.SyncAllProductPrices;

namespace GiftFinder.Application.Features.Products.Commands.SyncTrackedProductPrices;

public class SyncTrackedProductPricesResultDto
{
    public int TotalTrackedProducts { get; set; }
    public int TotalUpdated { get; set; }
    public int TotalUnchanged { get; set; }
    public int TotalFailed { get; set; }
    public int PriceDropsDetected { get; set; }
    public int NotificationsSent { get; set; }
    public double DurationSeconds { get; set; }
    public List<ProductSyncDetailDto> Details { get; set; } = new();
}
