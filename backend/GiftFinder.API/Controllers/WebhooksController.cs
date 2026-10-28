using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Affiliate.Commands.ProcessAccesstradeWebhook;
using GiftFinder.Application.Features.Affiliate.DTOs;
using GiftFinder.Application.Features.Subscriptions.Commands.ProcessPaymentWebhook;
using GiftFinder.Application.Features.Subscriptions.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IVnPayService _vnPayService;

    public WebhooksController(IMediator mediator, IVnPayService vnPayService)
    {
        _mediator = mediator;
        _vnPayService = vnPayService;
    }

    /// <summary>
    /// Nhận Postback webhook từ mạng lưới đối tác Accesstrade (Shopee & TikTok Shop)
    /// Tự động đối soát đơn hàng qua SubId, ghi nhận hoa hồng 5% - 10% và cập nhật trạng thái
    /// </summary>
    [HttpPost("accesstrade")]
    [AllowAnonymous]
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
    /// [UC-27] Tiếp nhận Webhook IPN ngầm từ Cổng thanh toán VNPAY (Server-to-Server)
    /// Tự động xác thực chữ ký HMAC-SHA512 (BR-29), đối soát Idempotent (BR-30), kích hoạt gói Premium hoặc Featured
    /// Trả về đúng format phản hồi quy định của VNPAY: {"RspCode": "00", "Message": "Confirm Success"}
    /// </summary>
    [HttpGet("vnpay-ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessVnPayIpn()
    {
        var queryDict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var ipnResult = _vnPayService.ParseIpn(queryDict);

        var command = new ProcessPaymentWebhookCommand
        {
            Payload = new PaymentWebhookPayload
            {
                TransactionId = ipnResult.TransactionId,
                OrderCode = ipnResult.TxnRef,
                Amount = ipnResult.Amount,
                Status = ipnResult.IsSuccess ? "PAID" : "FAILED",
                GatewayTransactionId = ipnResult.GatewayTransactionNo,
                Signature = queryDict.GetValueOrDefault("vnp_SecureHash"),
                RawParams = queryDict
            }
        };

        var result = await _mediator.Send(command);
        return Ok(new { RspCode = result.RspCode, Message = result.Message });
    }

    /// <summary>
    /// [UC-27] URL điều hướng người dùng quay lại ứng dụng sau khi hoàn tất thanh toán trên cổng VNPAY
    /// </summary>
    [HttpGet("vnpay-return")]
    [AllowAnonymous]
    public async Task<IActionResult> ProcessVnPayReturn()
    {
        var queryDict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var ipnResult = _vnPayService.ParseIpn(queryDict);

        var command = new ProcessPaymentWebhookCommand
        {
            Payload = new PaymentWebhookPayload
            {
                TransactionId = ipnResult.TransactionId,
                OrderCode = ipnResult.TxnRef,
                Amount = ipnResult.Amount,
                Status = ipnResult.IsSuccess ? "PAID" : "FAILED",
                GatewayTransactionId = ipnResult.GatewayTransactionNo,
                Signature = queryDict.GetValueOrDefault("vnp_SecureHash"),
                RawParams = queryDict
            }
        };

        var result = await _mediator.Send(command);

        return Ok(new
        {
            Success = ipnResult.IsSuccess && result.Success,
            TransactionId = ipnResult.TransactionId,
            OrderCode = ipnResult.TxnRef,
            Amount = ipnResult.Amount,
            BankCode = ipnResult.BankCode,
            GatewayTransactionNo = ipnResult.GatewayTransactionNo,
            Message = ipnResult.IsSuccess
                ? "Giao dịch thanh toán VNPAY thành công! Dịch vụ của bạn đã được kích hoạt."
                : "Giao dịch thanh toán không thành công hoặc đã bị hủy trên cổng VNPAY."
        });
    }
}
