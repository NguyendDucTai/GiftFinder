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
    
    // Thuộc tính tùy chọn: Cung hoàng đạo của người nhận (Dành cho Sinh nhật)
    public GiftFinder.Domain.Enums.ZodiacSign? RecipientZodiac { get; private set; }

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
        EventDate = eventDate.ToUniversalTime();
        RecipientRelation = recipientRelation?.Trim();
        DaysBeforeNotify = daysBeforeNotify;
        Note = note?.Trim();
        IsActive = true;

        // Tự động nhận diện sinh nhật để tính Cung Hoàng Đạo
        if (Title.ToLowerInvariant().Contains("sinh nhật") || Title.ToLowerInvariant().Contains("birthday"))
        {
            RecipientZodiac = ZodiacCalculator.GetZodiacSign(eventDate.Day, eventDate.Month);
        }
    }

    public void MarkNotified(DateTime notifiedAt)
    {
        LastNotifiedAt = notifiedAt;
    }

    public void Update(string title, DateTime eventDate, string? recipientRelation, int daysBeforeNotify, string? note)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Tiêu đề ngày kỷ niệm không được để trống.");

        Title = title.Trim();
        EventDate = eventDate.ToUniversalTime();
        RecipientRelation = recipientRelation?.Trim();
        DaysBeforeNotify = daysBeforeNotify;
        Note = note?.Trim();
        UpdatedAt = DateTime.UtcNow;

        // Cập nhật lại cung hoàng đạo nếu là sinh nhật
        if (Title.ToLowerInvariant().Contains("sinh nhật") || Title.ToLowerInvariant().Contains("birthday"))
        {
            RecipientZodiac = ZodiacCalculator.GetZodiacSign(eventDate.Day, eventDate.Month);
        }
        else
        {
            RecipientZodiac = null;
        }
    }
}
