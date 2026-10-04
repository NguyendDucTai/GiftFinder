using GiftFinder.Application.Features.Wishlists.Commands.Add;
using GiftFinder.Application.Features.Wishlists.Commands.Remove;
using GiftFinder.Application.Features.Wishlists.Queries.GetWishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WishlistsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách sản phẩm yêu thích (Wishlist) của người dùng hiện tại
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWishlist()
    {
        try
        {
            var result = await _mediator.Send(new GetWishlistQuery());
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Thêm sản phẩm vào Wishlist
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToWishlist([FromBody] AddToWishlistCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { Message = "Đã thêm vào danh sách yêu thích thành công.", WishlistId = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Lỗi hệ thống.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Xóa sản phẩm khỏi Wishlist
    /// </summary>
    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId)
    {
        try
        {
            await _mediator.Send(new RemoveFromWishlistCommand(productId));
            return Ok(new { Message = "Đã xóa sản phẩm khỏi danh sách yêu thích." });
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
}
