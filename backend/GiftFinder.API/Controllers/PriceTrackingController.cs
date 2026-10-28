using System;
using System.Threading.Tasks;
using GiftFinder.Application.Features.PriceTrackings.Commands.TrackProductPrice;
using GiftFinder.Application.Features.PriceTrackings.Commands.UntrackProductPrice;
using GiftFinder.Application.Features.PriceTrackings.Queries.GetMyTrackedProducts;
using GiftFinder.Application.Features.PriceTrackings.Queries.GetProductPriceHistory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[Route("api/price-tracking")]
[ApiController]
public class PriceTrackingController : ControllerBase
{
    private readonly IMediator _mediator;

    public PriceTrackingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// [UC-08] Lấy danh sách sản phẩm đang theo dõi giá của tôi (dành cho trang Quản lý theo dõi giá)
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyTrackedProducts([FromQuery] bool onlyPriceDropped = false)
    {
        try
        {
            var result = await _mediator.Send(new GetMyTrackedProductsQuery { OnlyPriceDropped = onlyPriceDropped });
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Lỗi hệ thống.", Details = ex.Message });
        }
    }

    /// <summary>
    /// [UC-08] Bắt đầu theo dõi biến động giá 1-chạm (Free: 3 món đầu, 1 món/ngày từ lần sau; Premium: 20 món)
    /// </summary>
    [HttpPost("{productId}")]
    [Authorize]
    public async Task<IActionResult> TrackProductPrice(Guid productId)
    {
        try
        {
            var result = await _mediator.Send(new TrackProductPriceCommand(productId));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new 
            { 
                Message = ex.Message,
                UpgradePrompt = "Nâng cấp gói Premium (chỉ 29.000đ/tháng) để theo dõi tối đa 20 sản phẩm không giới hạn!",
                UpgradeUrl = "/tai-khoan/nang-cap-premium"
            });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Lỗi hệ thống.", Details = ex.Message });
        }
    }

    /// <summary>
    /// [UC-08] Hủy theo dõi biến động giá của một sản phẩm
    /// </summary>
    [HttpDelete("{productId}")]
    [Authorize]
    public async Task<IActionResult> UntrackProductPrice(Guid productId)
    {
        try
        {
            await _mediator.Send(new UntrackProductPriceCommand(productId));
            return Ok(new { Message = "Đã hủy theo dõi biến động giá cho sản phẩm thành công." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Lỗi hệ thống.", Details = ex.Message });
        }
    }

    /// <summary>
    /// [UC-08] Xem lịch sử biến động giá của một sản phẩm (để vẽ biểu đồ giá)
    /// </summary>
    [HttpGet("{productId}/history")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPriceHistory(Guid productId)
    {
        try
        {
            var result = await _mediator.Send(new GetProductPriceHistoryQuery(productId));
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Lỗi hệ thống.", Details = ex.Message });
        }
    }
}
