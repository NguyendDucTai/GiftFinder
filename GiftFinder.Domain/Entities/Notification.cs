using GiftFinder.Domain.Common;
using GiftFinder.Domain.Enums;

namespace GiftFinder.Domain.Entities;

public class Notification : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public NotificationType Type { get; private set; } = NotificationType.System;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public string? ActionUrl { get; private set; }

    protected Notification() { }

    public Notification(
        Guid userId,
        string title,
        string content,
        NotificationType type = NotificationType.System,
        string? actionUrl = null)
    {
        UserId = userId;
        Title = title.Trim();
        Content = content.Trim();
        Type = type;
        ActionUrl = actionUrl;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
