namespace GiftFinder.Application.Features.ReminderDates.DTOs;

public class ReminderDateDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public DateTime EventDate { get; set; }
    public string? RecipientRelation { get; set; }
    public int DaysBeforeNotify { get; set; }
    public string? Note { get; set; }
    public string? RecipientZodiac { get; set; }
    public bool IsActive { get; set; }
}
