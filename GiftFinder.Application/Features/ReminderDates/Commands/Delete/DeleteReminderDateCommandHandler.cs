using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.ReminderDates.Commands.Delete;

public class DeleteReminderDateCommandHandler : IRequestHandler<DeleteReminderDateCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteReminderDateCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(DeleteReminderDateCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");

        var reminder = await _context.ReminderDates
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.UserId == userId, cancellationToken);

        if (reminder == null)
            throw new ArgumentException("Không tìm thấy ngày kỷ niệm hoặc bạn không có quyền xóa.");

        reminder.IsDeleted = true;
        reminder.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
