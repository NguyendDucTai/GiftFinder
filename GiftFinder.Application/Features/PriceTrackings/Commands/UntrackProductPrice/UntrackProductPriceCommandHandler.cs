using System;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.PriceTrackings.Commands.UntrackProductPrice;

public class UntrackProductPriceCommandHandler : IRequestHandler<UntrackProductPriceCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UntrackProductPriceCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(UntrackProductPriceCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId 
            ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập để thao tác.");

        var tracking = await _context.PriceTrackings
            .FirstOrDefaultAsync(pt => pt.UserId == userId && pt.ProductId == request.ProductId && pt.IsActive, cancellationToken);

        if (tracking == null)
        {
            throw new ArgumentException("Sản phẩm không có trong danh sách đang theo dõi của bạn.");
        }

        tracking.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
