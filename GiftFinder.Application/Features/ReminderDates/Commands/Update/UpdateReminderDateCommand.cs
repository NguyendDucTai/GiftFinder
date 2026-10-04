using MediatR;

namespace GiftFinder.Application.Features.ReminderDates.Commands.Update;

public record UpdateReminderDateCommand(
    Guid Id,
    string Title,
    DateTime EventDate,
    string RecipientRelation,
    int DaysBeforeNotify,
    string? Note
) : IRequest;
