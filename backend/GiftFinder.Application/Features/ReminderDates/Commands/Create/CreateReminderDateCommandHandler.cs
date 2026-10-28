using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.ReminderDates.Commands.Create;

public class CreateReminderDateCommandHandler : IRequestHandler<CreateReminderDateCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateReminderDateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateReminderDateCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");

        var count = await _context.ReminderDates.CountAsync(r => r.UserId == userId, cancellationToken);
        if (count >= 50)
        {
            throw new InvalidOperationException("Bạn đã đạt giới hạn 50 ngày kỷ niệm được lưu, vui lòng xóa bớt trước khi thêm mới.");
        }

        var reminder = new ReminderDate(
            userId,
            request.Title,
            request.EventDate,
            request.RecipientRelation,
            request.DaysBeforeNotify,
            request.Note
        );

        _context.ReminderDates.Add(reminder);
        await _context.SaveChangesAsync(cancellationToken);

        return reminder.Id;
    }
}
