using MediatR;

namespace GiftFinder.Application.Features.ReminderDates.Commands.Create;

public record CreateReminderDateCommand(
    string Title,
    DateTime EventDate,
    string RecipientRelation,
    int DaysBeforeNotify,
    string? Note
) : IRequest<Guid>;
