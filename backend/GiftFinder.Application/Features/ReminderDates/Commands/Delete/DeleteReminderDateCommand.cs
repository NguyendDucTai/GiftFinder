using MediatR;

namespace GiftFinder.Application.Features.ReminderDates.Commands.Delete;

public record DeleteReminderDateCommand(Guid Id) : IRequest;
