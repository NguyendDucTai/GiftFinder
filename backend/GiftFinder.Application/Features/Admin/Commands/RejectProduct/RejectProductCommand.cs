using FluentValidation;
using GiftFinder.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Admin.Commands.RejectProduct;

public class RejectProductCommand : IRequest<RejectProductResultDto>
{
    public Guid ProductId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RejectProductCommandValidator : AbstractValidator<RejectProductCommand>
{
    public RejectProductCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Mã sản phẩm không được để trống.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do từ chối không được để trống.")
            .MaximumLength(500).WithMessage("Lý do từ chối không được vượt quá 500 ký tự.");
    }
}

public class RejectProductResultDto
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RejectReason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class RejectProductCommandHandler : IRequestHandler<RejectProductCommand, RejectProductResultDto>
{
    private readonly IApplicationDbContext _context;

    public RejectProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RejectProductResultDto> Handle(RejectProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với mã '{request.ProductId}'.");
        }

        product.Reject(request.Reason.Trim());
        await _context.SaveChangesAsync(cancellationToken);

        return new RejectProductResultDto
        {
            ProductId = product.Id,
            Name = product.Name,
            Status = product.Status.ToString(),
            RejectReason = product.RejectReason ?? string.Empty,
            Message = "Đã từ chối duyệt sản phẩm kèm lý do phản hồi cho đối tác."
        };
    }
}
