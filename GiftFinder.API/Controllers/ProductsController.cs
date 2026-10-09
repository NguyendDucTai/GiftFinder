using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Products.Commands.TrackAffiliateClick;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ProductsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm trong kho (hỗ trợ phân trang, tìm kiếm và lọc theo sàn)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(
        [FromServices] IApplicationDbContext context,
        [FromQuery] string? keyword = null,
        [FromQuery] ProductSource? source = null,
        [FromQuery] ProductStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var cleanKeyword = keyword.Trim();
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{cleanKeyword}%"));
        }

        if (source.HasValue)
        {
            query = query.Where(p => p.Source == source.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var total = await query.CountAsync();
        var safePageSize = Math.Clamp(pageSize, 1, 100);
        var safePage = Math.Max(1, page);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.OriginalPrice,
                p.ImageUrl,
                p.OriginalUrl,
                p.AffiliateUrl,
                Source = p.Source.ToString(),
                Status = p.Status.ToString(),
                p.RejectReason,
                p.Rating,
                p.TotalReviews,
                p.CreatedAt,
                Tags = p.ProductTags.Select(pt => new
                {
                    pt.Tag.Id,
                    pt.Tag.Name,
                    Type = pt.Tag.Type.ToString()
                })
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            page = safePage,
            pageSize = safePageSize,
            items
        });
    }

    /// <summary>
    /// Lấy chi tiết 1 sản phẩm theo ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductById(Guid id, [FromServices] IApplicationDbContext context)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.ProductTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound(new { message = $"Không tìm thấy sản phẩm với Id: {id}" });

        return Ok(new
        {
            product.Id,
            product.Name,
            product.Description,
            product.Price,
            product.OriginalPrice,
            product.ImageUrl,
            product.OriginalUrl,
            product.AffiliateUrl,
            Source = product.Source.ToString(),
            Status = product.Status.ToString(),
            product.RejectReason,
            product.Rating,
            product.TotalReviews,
            product.CreatedAt,
            Tags = product.ProductTags.Select(pt => new
            {
                pt.Tag.Id,
                pt.Tag.Name,
                Type = pt.Tag.Type.ToString()
            })
        });
    }

    /// <summary>
    /// Ghi nhận click Affiliate, cộng điểm cho sản phẩm và trả về link chuyển hướng
    /// </summary>
    [HttpPost("{id}/click-affiliate")]
    [AllowAnonymous] // Cho phép khách vãng lai click mua hàng
    public async Task<IActionResult> TrackAffiliateClick(Guid id)
    {
        try
        {
            var command = new TrackAffiliateClickCommand
            {
                ProductId = id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString(),
                UserId = _currentUserService.UserId // Nếu có đăng nhập thì hệ thống sẽ tự bắt được
            };

            var targetUrl = await _mediator.Send(command);

            // Trả về JSON chứa URL để Frontend chủ động chuyển trang (window.open)
            return Ok(new { url = targetUrl });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
