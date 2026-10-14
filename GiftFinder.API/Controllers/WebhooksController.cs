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

    /// <summary>
    /// [UC-27] Tiếp nhận Webhook IPN từ cổng thanh toán (PayOS / VietQR / Ngân hàng)
    /// Tự động xác thực chữ ký, đối soát Idempotent, kích hoạt gói Premium (UC-14) và gửi email chúc mừng
    /// </summary>
    [HttpPost("payment")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessPaymentWebhook(
        [FromBody] GiftFinder.Application.Features.Subscriptions.DTOs.PaymentWebhookPayload payload)
    {
        var signature = Request.Headers["X-Signature"].ToString();
        if (!string.IsNullOrWhiteSpace(signature) && string.IsNullOrWhiteSpace(payload.Signature))
        {
            payload.Signature = signature;
        }

        var command = new GiftFinder.Application.Features.Subscriptions.Commands.ProcessPaymentWebhook.ProcessPaymentWebhookCommand
        {
            Payload = payload,
            RawJson = System.Text.Json.JsonSerializer.Serialize(payload)
        };

        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
