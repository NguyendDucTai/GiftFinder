using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class PriceHistory : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public long OldPrice { get; private set; }
    public long NewPrice { get; private set; }
    public DateTime ChangedAt { get; private set; } = DateTime.UtcNow;

    protected PriceHistory() { }

    public PriceHistory(Guid productId, long oldPrice, long newPrice)
    {
        ProductId = productId;
        OldPrice = oldPrice;
        NewPrice = newPrice;
        ChangedAt = DateTime.UtcNow;
    }
}
