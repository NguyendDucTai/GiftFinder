using GiftFinder.Application.Features.ReminderDates.Commands.Create;
using GiftFinder.Application.Features.ReminderDates.Commands.Update;
using GiftFinder.Application.Features.ReminderDates.Commands.Delete;
using GiftFinder.Application.Features.ReminderDates.Queries.GetReminderDates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReminderDatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReminderDatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách ngày kỷ niệm / sinh nhật
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReminderDates()
    {
        try
        {
            var result = await _mediator.Send(new GetReminderDatesQuery());
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
    /// Thêm mới ngày kỷ niệm
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateReminderDate([FromBody] CreateReminderDateCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { Message = "Đã thêm ngày kỷ niệm thành công.", ReminderDateId = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex) // Bắt lỗi giới hạn 50 bản ghi
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "Lỗi hệ thống.", Details = ex.InnerException?.Message ?? ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật thông tin ngày kỷ niệm
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReminderDate(Guid id, [FromBody] UpdateReminderDateCommand command)
    {
        try
        {
            if (id != command.Id)
                return BadRequest(new { Message = "ID trong URL và Body không khớp." });

            await _mediator.Send(command);
            return Ok(new { Message = "Đã cập nhật ngày kỷ niệm thành công." });
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
    /// Xóa ngày kỷ niệm
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReminderDate(Guid id)
    {
        try
        {
            await _mediator.Send(new DeleteReminderDateCommand(id));
            return Ok(new { Message = "Đã xóa ngày kỷ niệm thành công." });
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
