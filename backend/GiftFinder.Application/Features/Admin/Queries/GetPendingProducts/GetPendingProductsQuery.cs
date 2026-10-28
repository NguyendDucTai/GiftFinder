using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Admin.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Admin.Queries.GetPendingProducts;

public class GetPendingProductsQuery : IRequest<PagedAdminResult<PendingProductDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetPendingProductsQueryHandler : IRequestHandler<GetPendingProductsQuery, PagedAdminResult<PendingProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedAdminResult<PendingProductDto>> Handle(GetPendingProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Pending);

        var totalCount = await query.CountAsync(cancellationToken);

        var safePage = Math.Max(1, request.PageNumber);
        var safePageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .Include(p => p.Merchant)
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(p => new PendingProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                ImageUrl = p.ImageUrl,
                OriginalUrl = p.OriginalUrl,
                Source = p.Source.ToString(),
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                MerchantId = p.MerchantId,
                ShopName = p.Merchant != null ? p.Merchant.ShopName : null,
                ContactPhone = p.Merchant != null ? p.Merchant.ContactPhone : null,
                ContactEmail = p.Merchant != null ? p.Merchant.ContactEmail : null,
                TagNames = p.ProductTags.Select(pt => pt.Tag.Name).ToList(),
                SuitableZodiacs = p.SuitableZodiacs.Select(z => z.ToString()).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedAdminResult<PendingProductDto>
        {
            TotalCount = totalCount,
            PageNumber = safePage,
            PageSize = safePageSize,
            Items = items
        };
    }
}
