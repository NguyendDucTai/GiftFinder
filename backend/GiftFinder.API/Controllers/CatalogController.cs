using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Catalog.Queries.GetCatalogProducts;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;

    public CatalogController(IMediator mediator, IApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    /// <summary>
    /// Lọc sản phẩm cho trang Danh mục (Catalog) bằng luật Tag + Từ khóa cố định (0 token AI, hỗ trợ phân trang thật, không nhét bừa sản phẩm ngoài luật)
    /// </summary>
    [HttpGet("products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCatalogProducts(
        [FromQuery] string? category = "tat-ca",
        [FromQuery] string? keyword = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        [FromQuery] long? minPrice = null,
        [FromQuery] long? maxPrice = null,
        [FromQuery] string? sort = "relevance",
        [FromQuery] ProductSource? source = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCatalogProductsQuery
        {
            Category = category,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Sort = sort,
            Source = source
        };

        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy danh sách tất cả các danh mục kèm số lượng sản phẩm khớp thực tế (chỉ tính sản phẩm đã duyệt và có Tag)
    /// </summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
    {
        var handler = new GetCatalogProductsQueryHandler(_context);
        var categories = await handler.GetCategoriesWithCountAsync(cancellationToken);
        return Ok(categories);
    }

    /// <summary>
    /// Lấy danh sách sản phẩm liên quan ("Xem thêm") dựa trên Tag trùng, nhóm sản phẩm và mức giá tương đồng (0 token AI, không nhét bừa)
    /// </summary>
    [HttpGet("related/{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRelatedProducts(
        Guid productId,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var handler = new GetCatalogProductsQueryHandler(_context);
        var items = await handler.GetRelatedProductsAsync(productId, limit, cancellationToken);
        return Ok(items);
    }
}

