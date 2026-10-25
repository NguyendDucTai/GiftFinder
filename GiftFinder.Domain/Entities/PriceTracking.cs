using System;
using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class PriceTracking : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public long InitialPrice { get; private set; }
    public long? LastNotifiedPrice { get; private set; }
    public DateTime? LastNotifiedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    protected PriceTracking() { }

    public PriceTracking(Guid userId, Guid productId, long initialPrice)
    {
        if (initialPrice <= 0)
            throw new ArgumentException("Giá ban đầu phải là số nguyên dương.", nameof(initialPrice));

        UserId = userId;
        ProductId = productId;
        InitialPrice = initialPrice;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate(long currentPrice)
    {
        if (currentPrice <= 0)
            throw new ArgumentException("Giá hiện tại phải là số nguyên dương.", nameof(currentPrice));

        IsActive = true;
        InitialPrice = currentPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkNotified(long price)
    {
        LastNotifiedPrice = price;
        LastNotifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
