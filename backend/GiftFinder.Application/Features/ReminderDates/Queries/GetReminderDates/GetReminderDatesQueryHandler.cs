using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.ReminderDates.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.ReminderDates.Queries.GetReminderDates;

public class GetReminderDatesQueryHandler : IRequestHandler<GetReminderDatesQuery, List<ReminderDateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetReminderDatesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<ReminderDateDto>> Handle(GetReminderDatesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");

        var reminders = await _context.ReminderDates
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.EventDate)
            .ToListAsync(cancellationToken);

        return reminders.Select(r => new ReminderDateDto
        {
            Id = r.Id,
            Title = r.Title,
            EventDate = r.EventDate,
            RecipientRelation = r.RecipientRelation,
            DaysBeforeNotify = r.DaysBeforeNotify,
            Note = r.Note,
            RecipientZodiac = r.RecipientZodiac?.ToString(),
            IsActive = r.IsActive
        }).ToList();
    }
}
