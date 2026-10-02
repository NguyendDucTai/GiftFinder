using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class ClickTracking : BaseEntity
{
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = default!;

    public string SubId { get; private set; } = default!;
    public string TrackingUrl { get; private set; } = default!;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTime ClickedAt { get; private set; } = DateTime.UtcNow;
    public ClickStatus Status { get; private set; } = ClickStatus.Clicked;

    protected ClickTracking() { }

    public ClickTracking(
        Guid productId,
        string subId,
        string trackingUrl,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (string.IsNullOrWhiteSpace(subId))
            throw new ArgumentException("SubId tracking không được để trống.");
        if (string.IsNullOrWhiteSpace(trackingUrl))
            throw new ArgumentException("Tracking URL không được để trống.");

        ProductId = productId;
        SubId = subId.Trim();
        TrackingUrl = trackingUrl.Trim();
        UserId = userId;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        ClickedAt = DateTime.UtcNow;
        Status = ClickStatus.Clicked;
    }

    public void MarkConverted()
    {
        Status = ClickStatus.Converted;
    }
}