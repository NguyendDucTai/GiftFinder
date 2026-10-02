using GiftFinder.Domain.Common;

namespace GiftFinder.Domain.Entities;

public class ReminderDate : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public User User { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public DateTime EventDate { get; private set; }
    public string? RecipientRelation { get; private set; }
    public int DaysBeforeNotify { get; private set; } = 3;
    public bool IsActive { get; private set; } = true;
    public string? Note { get; private set; }
    public DateTime? LastNotifiedAt { get; private set; }

    protected ReminderDate() { }

    public ReminderDate(
        Guid userId,
        string title,
        DateTime eventDate,
        string? recipientRelation = null,
        int daysBeforeNotify = 3,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề ngày kỷ niệm không được để trống.");

        UserId = userId;
        Title = title.Trim();
        EventDate = eventDate;
        RecipientRelation = recipientRelation?.Trim();
        DaysBeforeNotify = daysBeforeNotify;
        Note = note?.Trim();
        IsActive = true;
    }

    public void MarkNotified(DateTime notifiedAt)
    {
        LastNotifiedAt = notifiedAt;
    }
}
