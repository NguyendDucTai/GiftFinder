using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Commands.DeleteMerchantProduct;

public class DeleteMerchantProductCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public DeleteMerchantProductCommand(Guid id)
    {
        Id = id;
    }
}

public class DeleteMerchantProductCommandHandler : IRequestHandler<DeleteMerchantProductCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteMerchantProductCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(DeleteMerchantProductCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thao tác.");
        }

        var merchant = await _context.Merchants
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new InvalidOperationException("Tài khoản của bạn chưa đăng ký gian hàng.");
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.MerchantId == merchant.Id, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm trong gian hàng của bạn.");
        }

        // Chuyển trạng thái sang Inactive (Ẩn sản phẩm khỏi sàn)
        product.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
