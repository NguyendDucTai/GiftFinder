using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.GiftPolls.Commands.Delete;

public class DeleteGiftPollCommandHandler : IRequestHandler<DeleteGiftPollCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteGiftPollCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteGiftPollCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để thực hiện thao tác.");

        var giftPoll = await _context.GiftPolls.FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);
        if (giftPoll == null)
            throw new ArgumentException("Cuộc bình chọn không tồn tại.");

        if (giftPoll.UserId != userId)
            throw new UnauthorizedAccessException("Bạn không có quyền xóa cuộc bình chọn này.");

        giftPoll.IsDeleted = true;
        giftPoll.SetActive(false);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
