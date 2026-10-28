using GiftFinder.Application.Features.Subscriptions.Commands.CancelAutoRenew;
using GiftFinder.Application.Features.Subscriptions.Commands.CreateSubscriptionPayment;
using GiftFinder.Application.Features.Subscriptions.Commands.SimulatePaymentSuccess;
using GiftFinder.Application.Features.Subscriptions.Queries.GetSubscriptionStatus;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiftFinder.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SubscriptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// [UC-14 & UC-24] Khởi tạo giao dịch nâng cấp gói thành viên Premium qua Cổng thanh toán VNPAY (15.000đ/tháng)
    /// Trả về đường link chuyển hướng sang VNPAY Sandbox chuẩn mã hóa HMAC-SHA512
    /// </summary>
    [HttpPost("upgrade")]
    [Authorize]
    public async Task<IActionResult> Upgrade([FromBody] CreateSubscriptionPaymentCommand command)
    {
        command.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Xem thông tin chi tiết và trạng thái gói thành viên của tài khoản đang đăng nhập
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public async Task<IActionResult> GetStatus()
    {
        var result = await _mediator.Send(new GetSubscriptionStatusQuery());
        return Ok(result);
    }

    /// <summary>
    /// Hủy chế độ tự động gia hạn trước kỳ thanh toán tiếp theo
    /// </summary>
    [HttpPost("cancel-auto-renew")]
    [Authorize]
    public async Task<IActionResult> CancelAutoRenew()
    {
        var result = await _mediator.Send(new CancelAutoRenewCommand());
        return Ok(new
        {
            success = result,
            message = "Đã tắt tính năng tự động gia hạn gói Premium thành công."
        });
    }

    /// <summary>
    /// [Dành cho Môi trường Test / Dev / Swagger] Giả lập thanh toán VietQR thành công ngay lập tức
    /// Kích hoạt gói Premium và gửi email chúc mừng qua Brevo SMTP mà không cần quét mã chuyển khoản thật
    /// </summary>
    [HttpPost("test-pay/{transactionId}")]
    [AllowAnonymous]
    public async Task<IActionResult> SimulatePaymentSuccess(Guid transactionId)
    {
        var command = new SimulatePaymentSuccessCommand { TransactionId = transactionId };
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
