using GiftFinder.Application.Features.GiftPolls.Commands.Create;
using GiftFinder.Application.Features.GiftPolls.Commands.Delete;
using GiftFinder.Application.Features.GiftPolls.Commands.Update;
using GiftFinder.Application.Features.GiftPolls.Commands.Vote;
using GiftFinder.Application.Features.GiftPolls.Queries.GetMyPolls;
using GiftFinder.Application.Features.GiftPolls.Queries.GetPublicPoll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GiftPollsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GiftPollsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// [Đăng nhập] Tạo một cuộc bình chọn quà tặng mới (chọn từ 1 đến 10 món quà)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateGiftPoll([FromBody] CreateGiftPollCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Đăng nhập] Lấy danh sách tất cả các cuộc bình chọn do tôi đã tạo
    /// </summary>
    [HttpGet("my-polls")]
    [Authorize]
    public async Task<IActionResult> GetMyPolls()
    {
        try
        {
            var result = await _mediator.Send(new GetMyGiftPollsQuery());
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Đăng nhập] Cập nhật cuộc bình chọn: sửa tiêu đề, lời nhắn, thêm/xóa các món quà
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateGiftPoll(Guid id, [FromBody] UpdateGiftPollCommand command)
    {
        try
        {
            command.Id = id;
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Đăng nhập] Xóa / Hủy cuộc bình chọn
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteGiftPoll(Guid id)
    {
        try
        {
            var result = await _mediator.Send(new DeleteGiftPollCommand(id));
            return Ok(new { success = result, message = "Đã xóa cuộc bình chọn thành công." });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Công khai - Không cần đăng nhập] Xem thông tin và danh sách quà trong cuộc bình chọn qua mã ShareCode
    /// </summary>
    [HttpGet("share/{shareCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicPoll(string shareCode)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _mediator.Send(new GetPublicGiftPollQuery(shareCode, ipAddress));
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Liên kết đã hết hạn sau 30 ngày (BR-05) hoặc đã đóng
            return StatusCode(StatusCodes.Status410Gone, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Công khai - Không cần đăng nhập] Bình chọn 1 chạm cho món quà (Chống spam bằng IP)
    /// </summary>
    [HttpPost("share/{shareCode}/vote")]
    [AllowAnonymous]
    public async Task<IActionResult> VoteGiftPoll(string shareCode, [FromBody] VoteRequest request)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
            var userAgent = Request.Headers.UserAgent.ToString();

            var command = new VoteGiftPollCommand
            {
                ShareCode = shareCode,
                ProductId = request.ProductId,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status410Gone, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class VoteRequest
{
    public Guid ProductId { get; set; }
}
