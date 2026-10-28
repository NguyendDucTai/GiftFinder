using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Admin.Commands.ApproveProduct;

public class ApproveProductCommand : IRequest<ApproveProductResultDto>
{
    public Guid ProductId { get; set; }

    public ApproveProductCommand(Guid productId)
    {
        ProductId = productId;
    }
}

public class ApproveProductResultDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ApproveProductCommandHandler : IRequestHandler<ApproveProductCommand, ApproveProductResultDto>
{
    private readonly IApplicationDbContext _context;

    public ApproveProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApproveProductResultDto> Handle(ApproveProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với mã '{request.ProductId}'.");
        }

        if (product.Status == ProductStatus.Approved)
        {
            return new ApproveProductResultDto
            {
                ProductId = product.Id,
                Name = product.Name,
                Status = product.Status.ToString(),
                Message = "Sản phẩm này đã được duyệt trước đó."
            };
        }

        product.Approve();
        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveProductResultDto
        {
            ProductId = product.Id,
            Name = product.Name,
            Status = product.Status.ToString(),
            Message = "Phê duyệt sản phẩm thành công! Sản phẩm đã xuất hiện trên kho công khai và sẵn sàng cho AI gợi ý."
        };
    }
}
