using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.ReminderDates.Commands.Update;

public class UpdateReminderDateCommandHandler : IRequestHandler<UpdateReminderDateCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateReminderDateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateReminderDateCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");

        var reminder = await _context.ReminderDates
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == userId, cancellationToken);

        if (reminder == null)
            throw new ArgumentException("Không tìm thấy ngày kỷ niệm hoặc bạn không có quyền sửa.");

        reminder.Update(
            request.Title,
            request.EventDate,
            request.RecipientRelation,
            request.DaysBeforeNotify,
            request.Note
        );

        await _context.SaveChangesAsync(cancellationToken);
    }
}
