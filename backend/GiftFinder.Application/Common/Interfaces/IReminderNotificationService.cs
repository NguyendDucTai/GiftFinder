namespace GiftFinder.Application.Common.Interfaces;

public interface IReminderNotificationService
{
    Task ProcessUpcomingRemindersAsync(CancellationToken cancellationToken = default);
}
