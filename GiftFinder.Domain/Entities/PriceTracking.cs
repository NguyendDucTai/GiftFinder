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

    // Quản lý vòng đời (Lifecycle & Expiration)
    public DateTime? ExpiresAt { get; private set; } // Ngày hết hạn (Free: CreatedAt + 14 ngày; Premium: null)
    public bool IsLocked { get; private set; } = false; // Trạng thái khóa mờ
    public string? LockReason { get; private set; } // "TrialExpired" hoặc "PremiumExpired"

    protected PriceTracking() { }

    public PriceTracking(Guid userId, Guid productId, long initialPrice, DateTime? expiresAt = null)
    {
        if (initialPrice <= 0)
            throw new ArgumentException("Giá ban đầu phải là số nguyên dương.", nameof(initialPrice));

        UserId = userId;
        ProductId = productId;
        InitialPrice = initialPrice;
        ExpiresAt = expiresAt;
        IsActive = true;
        IsLocked = false;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ExpireTrial()
    {
        IsActive = false;
        IsLocked = true;
        LockReason = "TrialExpired";
        UpdatedAt = DateTime.UtcNow;
    }

    public void LockDueToPremiumExpiration()
    {
        IsActive = false;
        IsLocked = true;
        LockReason = "PremiumExpired";
        UpdatedAt = DateTime.UtcNow;
    }

    public void UnlockAfterPremiumRenewal()
    {
        IsActive = true;
        IsLocked = false;
        LockReason = null;
        ExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate(long currentPrice, DateTime? expiresAt = null)
    {
        if (currentPrice <= 0)
            throw new ArgumentException("Giá hiện tại phải là số nguyên dương.", nameof(currentPrice));

        IsActive = true;
        IsLocked = false;
        LockReason = null;
        InitialPrice = currentPrice;
        ExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckAndExpire(DateTime now)
    {
        if (IsActive && ExpiresAt.HasValue && ExpiresAt.Value <= now)
        {
            ExpireTrial();
        }
    }

    public void MarkNotified(long price)
    {
        LastNotifiedPrice = price;
        LastNotifiedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
