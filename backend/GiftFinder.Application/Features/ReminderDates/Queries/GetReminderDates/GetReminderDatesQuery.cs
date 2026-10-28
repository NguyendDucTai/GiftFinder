using GiftFinder.Application.Features.ReminderDates.DTOs;
using MediatR;

namespace GiftFinder.Application.Features.ReminderDates.Queries.GetReminderDates;

public record GetReminderDatesQuery() : IRequest<List<ReminderDateDto>>;
