using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Subscriptions.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Application.Features.Subscriptions.Commands.ProcessPaymentWebhook;

public class ProcessPaymentWebhookCommand : IRequest<PaymentWebhookResultDto>
{
    public PaymentWebhookPayload Payload { get; set; } = default!;
    public string RawJson { get; set; } = string.Empty;
}

public class ProcessPaymentWebhookCommandHandler : IRequestHandler<ProcessPaymentWebhookCommand, PaymentWebhookResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;

    public ProcessPaymentWebhookCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        IEmailService emailService,
        ILogger<ProcessPaymentWebhookCommandHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PaymentWebhookResultDto> Handle(ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;
        if (payload == null)
        {
            return new PaymentWebhookResultDto
            {
                Success = false,
                Message = "Dữ liệu webhook không hợp lệ."
            };
        }

        // 1. Xác thực chữ ký/checksum từ cổng thanh toán (BR-29)
        if (!string.IsNullOrWhiteSpace(payload.Signature))
        {
            var isValidSignature = _paymentService.VerifyWebhookSignature(request.RawJson, payload.Signature);
            if (!isValidSignature)
            {
                _logger.LogWarning("Webhook thanh toán bị từ chối do chữ ký không hợp lệ.");
                return new PaymentWebhookResultDto
                {
                    Success = false,
                    Message = "Chữ ký webhook không hợp lệ hoặc không xác thực được nguồn gửi."
                };
            }
        }

        // 2. Định danh giao dịch Transaction trong hệ thống
        Transaction? transaction = null;

        if (payload.TransactionId.HasValue)
        {
            transaction = await _context.Transactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == payload.TransactionId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(payload.OrderCode))
        {
            var code = payload.OrderCode.Trim();
            // Thử parse GUID từ order code
            if (Guid.TryParse(code, out var txnId))
            {
                transaction = await _context.Transactions
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == txnId, cancellationToken);
            }
            else
            {
                // Tìm theo chuỗi con trong Description hoặc Id
                var shortId = code.Replace("GFPREMIUM", "", StringComparison.OrdinalIgnoreCase).Trim();
                transaction = await _context.Transactions
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id.ToString().StartsWith(shortId), cancellationToken);
            }
        }

        if (transaction == null)
        {
            _logger.LogWarning("Không tìm thấy giao dịch nào khớp với mã: {OrderCode} / {TxnId}", payload.OrderCode, payload.TransactionId);
            return new PaymentWebhookResultDto
            {
                Success = false,
                Message = "Không tìm thấy giao dịch tương ứng trong hệ thống."
            };
        }

        // 3. Xử lý Idempotent: Nếu đơn hàng đã hoàn tất trước đó thì không xử lý lại (BR-30)
        if (transaction.Status == PaymentStatus.Completed)
        {
            return new PaymentWebhookResultDto
            {
                Success = true,
                TransactionId = transaction.Id,
                Status = transaction.Status.ToString(),
                Message = "Giao dịch này đã được kích hoạt thành công trước đó."
            };
        }

        var statusUpper = (payload.Status ?? "PAID").ToUpperInvariant();

        // 4. Nếu thanh toán thất bại hoặc hủy
        if (statusUpper == "FAILED" || statusUpper == "CANCELLED" || statusUpper == "EXPIRED")
        {
            transaction.Fail();
            await _context.SaveChangesAsync(cancellationToken);

            return new PaymentWebhookResultDto
            {
                Success = false,
                TransactionId = transaction.Id,
                Status = transaction.Status.ToString(),
                Message = "Thanh toán không thành công, vui lòng thử lại hoặc chọn phương thức thanh toán khác."
            };
        }

        // 5. Thanh toán thành công -> Hoàn tất giao dịch & Nâng cấp tài khoản User
        var gatewayTxnId = payload.GatewayTransactionId ?? $"GATEWAY_{Guid.NewGuid():N}"[..16];
        transaction.Complete(gatewayTxnId);

        // Tính số tháng nâng cấp dựa trên số tiền giao dịch
        int months = 1;
        if (transaction.Amount >= 150_000) months = 12;
        else if (transaction.Amount >= 45_000) months = 3;

        var user = transaction.User;
        if (user != null)
        {
            user.UpgradeToPremium(months, autoRenew: true);

            // Ghi nhận bản ghi thông báo hệ thống
            var notification = new Notification(
                user.Id,
                title: "Nâng cấp Premium thành công",
                content: "Chúc mừng, tài khoản của bạn đã được nâng cấp lên Premium.",
                type: NotificationType.System,
                actionUrl: "/account/subscription"
            );
            _context.Notifications.Add(notification);

            // Gửi email chúc mừng qua Brevo SMTP
            try
            {
                var expiresDateFormatted = user.PremiumExpiresAt?.ToString("dd/MM/yyyy") ?? "30 ngày tới";
                var htmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family: Arial, sans-serif; background-color: #f7f9fa; margin: 0; padding: 20px;'>
  <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.06); border: 1px solid #e1e8ed;'>
    <div style='background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%); padding: 30px; text-align: center; color: #ffffff;'>
      <h1 style='margin: 0; font-size: 24px; font-weight: bold;'>🎉 CHÚC MỪNG THÀNH VIÊN PREMIUM!</h1>
      <p style='margin: 10px 0 0; opacity: 0.9;'>Nâng tầm trải nghiệm tìm kiếm & tặng quà thông minh cùng GiftFinder</p>
    </div>
    <div style='padding: 30px; color: #333333; line-height: 1.6;'>
      <p>Xin chào <strong>{user.FullName}</strong>,</p>
      <p style='font-size: 16px; color: #16a34a; font-weight: bold;'>Chúc mừng, tài khoản của bạn đã được nâng cấp lên Premium.</p>
      <div style='background: #f8fafc; border-left: 4px solid #6366f1; padding: 15px; border-radius: 4px; margin: 20px 0;'>
        <p style='margin: 4px 0;'><strong>Gói thành viên:</strong> Premium VIP ({months} tháng)</p>
        <p style='margin: 4px 0;'><strong>Số tiền thanh toán:</strong> {transaction.Amount:N0} VNĐ</p>
        <p style='margin: 4px 0;'><strong>Thời hạn sử dụng đến:</strong> {expiresDateFormatted}</p>
        <p style='margin: 4px 0;'><strong>Mã giao dịch:</strong> {transaction.Id}</p>
      </div>
      <p><strong>🌟 Quyền lợi đặc quyền của bạn:</strong></p>
      <ul style='color: #475569;'>
        <li>Nhắc lịch sinh nhật, kỷ niệm tự động không giới hạn (gửi email kèm quà tặng).</li>
        <li>AI tư vấn cá nhân hóa chuyên sâu dựa trên sở thích và cung hoàng đạo.</li>
        <li>Theo dõi biến động giá quà tặng và nhận thông báo khi có ưu đãi giảm giá.</li>
      </ul>
      <div style='text-align: center; margin-top: 30px;'>
        <a href='https://localhost:7276/swagger/index.html' style='background: #6366f1; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>Trải nghiệm Premium ngay</a>
      </div>
    </div>
  </div>
</body>
</html>";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "[GiftFinder] Chúc mừng bạn đã nâng cấp thành công gói Premium!",
                    htmlBody,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gửi email chúc mừng Premium thất bại cho user {Email}", user.Email);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Nâng cấp thành công gói Premium cho User {UserId}, Giao dịch {TxnId}, Số tiền {Amount:N0}đ", 
            transaction.UserId, transaction.Id, transaction.Amount);

        return new PaymentWebhookResultDto
        {
            Success = true,
            TransactionId = transaction.Id,
            Status = transaction.Status.ToString(),
            Message = "Chúc mừng, tài khoản của bạn đã được nâng cấp lên Premium."
        };
    }
}
