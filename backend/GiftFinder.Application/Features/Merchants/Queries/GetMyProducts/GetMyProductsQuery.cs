using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.Application.Features.Merchants.Queries.GetMyProducts;

public class GetMyProductsQuery : IRequest<PagedResult<MerchantProductListItemDto>>
{
    public ProductStatus? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetMyProductsQueryHandler : IRequestHandler<GetMyProductsQuery, PagedResult<MerchantProductListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetMyProductsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PagedResult<MerchantProductListItemDto>> Handle(GetMyProductsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để xem danh sách sản phẩm.");
        }

        var merchant = await _context.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == currentUserId.Value, cancellationToken);

        if (merchant == null)
        {
            throw new KeyNotFoundException("Không tìm thấy thông tin gian hàng.");
        }

        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.MerchantId == merchant.Id);

        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var safePage = Math.Max(1, request.PageNumber);
        var safePageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(p => new MerchantProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                OriginalPrice = p.OriginalPrice,
                ImageUrl = p.ImageUrl,
                OriginalUrl = p.OriginalUrl,
                Status = p.Status.ToString(),
                RejectReason = p.RejectReason,
                IsFeatured = p.IsFeatured,
                CreatedAt = p.CreatedAt,
                TagNames = p.ProductTags.Select(pt => pt.Tag.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<MerchantProductListItemDto>
        {
            TotalCount = totalCount,
            PageNumber = safePage,
            PageSize = safePageSize,
            Items = items
        };
    }
}
