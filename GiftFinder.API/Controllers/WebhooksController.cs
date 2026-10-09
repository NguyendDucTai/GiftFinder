using GiftFinder.Application.Features.Affiliate.Commands.ProcessAccesstradeWebhook;
using GiftFinder.Application.Features.Affiliate.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public WebhooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Nhận Postback webhook từ mạng lưới đối tác Accesstrade (Shopee & TikTok Shop)
    /// Tự động đối soát đơn hàng qua SubId, ghi nhận hoa hồng 5% - 10% và cập nhật trạng thái
    /// </summary>
    [HttpPost("accesstrade")]
    [AllowAnonymous] // Webhook do sàn/mạng lưới đối tác gửi về nên không dùng Bearer token người dùng
    public async Task<IActionResult> ProcessAccesstradePostback([FromBody] AccesstradeWebhookPayload payload)
    {
        var command = new ProcessAccesstradeWebhookCommand
        {
            Payload = payload
        };

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
