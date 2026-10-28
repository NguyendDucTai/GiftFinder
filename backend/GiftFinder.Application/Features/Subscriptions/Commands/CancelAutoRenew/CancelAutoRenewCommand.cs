using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Subscriptions.Commands.CancelAutoRenew;

public class CancelAutoRenewCommand : IRequest<bool>
{
}

public class CancelAutoRenewCommandHandler : IRequestHandler<CancelAutoRenewCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CancelAutoRenewCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(CancelAutoRenewCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin tài khoản.");
        }

        user.CancelAutoRenew();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
