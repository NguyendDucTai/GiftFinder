using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GiftFinder.Application.Common.Interfaces;

public class PriceDropInfo
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public long OldPrice { get; set; }
    public long NewPrice { get; set; }
    public double DropPercent => OldPrice > 0 ? Math.Round(((OldPrice - NewPrice) / (double)OldPrice) * 100, 1) : 0;
}

public interface IPriceAlertService
{
    Task<int> ProcessPriceDropsAsync(IReadOnlyList<PriceDropInfo> priceDrops, CancellationToken cancellationToken = default);
}
