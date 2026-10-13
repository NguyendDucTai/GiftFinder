using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Queries.GetMerchantProductById;

public class GetMerchantProductByIdQuery : IRequest<MerchantProductDto>
{
    public Guid Id { get; set; }

    public GetMerchantProductByIdQuery(Guid id)
    {
        Id = id;
    }
}

public class GetMerchantProductByIdQueryHandler : IRequestHandler<GetMerchantProductByIdQuery, MerchantProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMerchantProductByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<MerchantProductDto> Handle(GetMerchantProductByIdQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem chi tiết sản phẩm.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == request.Id && p.MerchantId == merchant.Id, cancellationToken);

        if (product == null)
        {
            throw new KeyNotFoundException("Không tìm thấy sản phẩm trong gian hàng của bạn.");
        }

        return new MerchantProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            OriginalPrice = product.OriginalPrice,
            ImageUrl = product.ImageUrl,
            OriginalUrl = product.OriginalUrl,
            Source = product.Source.ToString(),
            Status = product.Status.ToString(),
            RejectReason = product.RejectReason,
            IsFeatured = product.IsFeatured,
            FeaturedUntil = product.FeaturedUntil,
            SuitableZodiacs = product.SuitableZodiacs.Select(z => z.ToString()).ToList(),
            TagIds = product.ProductTags.Select(pt => pt.TagId).ToList(),
            TagNames = product.ProductTags.Select(pt => pt.Tag.Name).ToList(),
            CreatedAt = product.CreatedAt
        };
    }
}
