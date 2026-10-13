using GiftFinder.Application.Features.Admin.Commands.ApproveProduct;
using GiftFinder.Application.Features.Admin.Commands.RejectProduct;
using GiftFinder.Application.Features.Admin.Commands.UserModeration;
using GiftFinder.Application.Features.Admin.Queries.GetPendingProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// [UC-28] Lấy danh sách toàn bộ sản phẩm quà tặng đang chờ duyệt (Pending) từ các Shop
    /// </summary>
    [HttpGet("products/pending")]
    public async Task<IActionResult> GetPendingProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPendingProductsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// [UC-28] Admin phê duyệt sản phẩm (Chuyển trạng thái sang Approved, xuất hiện trong AI Recommendations)
    /// </summary>
    [HttpPost("products/{id}/approve")]
    public async Task<IActionResult> ApproveProduct(Guid id)
    {
        var result = await _mediator.Send(new ApproveProductCommand(id));
        return Ok(result);
    }

    /// <summary>
    /// [UC-28] Admin từ chối phê duyệt sản phẩm kèm lý do (nhập reason tối đa 500 ký tự)
    /// </summary>
    [HttpPost("products/{id}/reject")]
    public async Task<IActionResult> RejectProduct(Guid id, [FromBody] RejectProductCommand command)
    {
        if (id != command.ProductId)
        {
            command.ProductId = id;
        }
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// [UC-29] Admin khóa tài khoản người dùng hoặc đối tác vi phạm/spam (kèm lý do)
    /// </summary>
    [HttpPost("users/{id}/lock")]
    public async Task<IActionResult> LockUser(Guid id, [FromBody] LockUserCommand command)
    {
        if (id != command.UserId)
        {
            command.UserId = id;
        }
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// [UC-29] Admin mở khóa tài khoản người dùng hoặc đối tác
    /// </summary>
    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid id)
    {
        var result = await _mediator.Send(new UnlockUserCommand(id));
        return Ok(result);
    }
}
