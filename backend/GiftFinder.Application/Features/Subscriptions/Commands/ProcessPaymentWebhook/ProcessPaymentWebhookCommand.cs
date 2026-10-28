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
    private readonly IVnPayService _vnPayService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;

    public ProcessPaymentWebhookCommandHandler(
        IApplicationDbContext context,
        IVnPayService vnPayService,
        IEmailService emailService,
        ILogger<ProcessPaymentWebhookCommandHandler> logger)
    {
        _context = context;
        _vnPayService = vnPayService;
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
                RspCode = "99",
                Message = "Dữ liệu webhook không hợp lệ."
            };
        }

        // 1. Xác thực chữ ký Checksum HMAC-SHA512 từ VNPAY (BR-29)
        if (payload.RawParams != null && !string.IsNullOrWhiteSpace(payload.Signature))
        {
            var isValidSignature = _vnPayService.ValidateSignature(payload.RawParams, payload.Signature);
            if (!isValidSignature)
            {
                _logger.LogWarning("[VnPay IPN] Chữ ký Checksum HMAC-SHA512 không hợp lệ từ VNPAY.");
                return new PaymentWebhookResultDto
                {
                    Success = false,
                    RspCode = "97",
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
                .Include(t => t.Product)
                .FirstOrDefaultAsync(t => t.Id == payload.TransactionId.Value, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(payload.OrderCode))
        {
            var code = payload.OrderCode.Trim();
            if (Guid.TryParse(code, out var txnId))
            {
                transaction = await _context.Transactions
                    .Include(t => t.User)
                    .Include(t => t.Product)
                    .FirstOrDefaultAsync(t => t.Id == txnId, cancellationToken);
            }
            else if (code.Length == 32 && Guid.TryParseExact(code, "N", out var txnIdExact))
            {
                transaction = await _context.Transactions
                    .Include(t => t.User)
                    .Include(t => t.Product)
                    .FirstOrDefaultAsync(t => t.Id == txnIdExact, cancellationToken);
            }
        }

        if (transaction == null)
        {
            _logger.LogWarning("[VnPay IPN] Không tìm thấy giao dịch nào khớp với mã: {OrderCode} / {TxnId}", payload.OrderCode, payload.TransactionId);
            return new PaymentWebhookResultDto
            {
                Success = false,
                RspCode = "01",
                Message = "Không tìm thấy giao dịch tương ứng trong hệ thống."
            };
        }

        // 3. Xử lý Idempotent: Nếu đơn hàng đã hoàn tất trước đó thì không xử lý lại (BR-30)
        if (transaction.Status == PaymentStatus.Completed)
        {
            return new PaymentWebhookResultDto
            {
                Success = true,
                RspCode = "02",
                TransactionId = transaction.Id,
                Status = transaction.Status.ToString(),
                Message = "Giao dịch này đã được kích hoạt thành công trước đó."
            };
        }

        // 4. Kiểm tra số tiền khớp với dữ liệu giao dịch nội bộ
        if (payload.Amount > 0 && payload.Amount != transaction.Amount)
        {
            _logger.LogWarning("[VnPay IPN] Số tiền không khớp cho giao dịch {TxnId}: Nhận {Received}đ != Kế hoạch {Expected}đ",
                transaction.Id, payload.Amount, transaction.Amount);
            return new PaymentWebhookResultDto
            {
                Success = false,
                RspCode = "04",
                Message = "Số tiền thanh toán không khớp với giao dịch."
            };
        }

        var statusUpper = (payload.Status ?? "PAID").ToUpperInvariant();

        // 5. Nếu thanh toán thất bại hoặc hủy trên cổng VNPAY
        if (statusUpper == "FAILED" || statusUpper == "CANCELLED" || statusUpper == "EXPIRED" || statusUpper == "24")
        {
            transaction.Fail();
            await _context.SaveChangesAsync(cancellationToken);

            return new PaymentWebhookResultDto
            {
                Success = false,
                RspCode = "00",
                TransactionId = transaction.Id,
                Status = transaction.Status.ToString(),
                Message = "Thanh toán không thành công, gói hiển thị nổi bật hoặc Premium chưa được kích hoạt."
            };
        }

        // 6. Thanh toán thành công -> Hoàn tất giao dịch
        var gatewayTxnId = payload.GatewayTransactionId ?? $"VNPAY_{Guid.NewGuid():N}"[..16];
        transaction.Complete(gatewayTxnId);

        // 7. Phân nhánh kích hoạt dịch vụ tự động theo loại giao dịch
        if (transaction.Type == TransactionType.FeaturedPlacement)
        {
            // =========================================================================
            // NHÁNH B: GÓI FEATURED PLACEMENT CHO SHOP (UC-17, UC-25)
            // =========================================================================
            var product = transaction.Product ?? await _context.Products.FirstOrDefaultAsync(p => p.Id == transaction.ProductId, cancellationToken);
            if (product != null)
            {
                var plan = transaction.FeaturedPlan ?? FeaturedPlanType.Weekly;
                // Kích hoạt cờ IsFeatured = true, tự động hỗ trợ gia hạn nối tiếp nếu gói cũ còn hạn (AC-4)
                product.ActivateFeatured(plan, DateTime.UtcNow);

                var planName = plan == FeaturedPlanType.Weekly ? "Gói Tuần (7 ngày)" : "Gói Tháng (30 ngày)";
                var untilFormatted = product.FeaturedUntil?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";

                // Tạo thông báo In-app cho Merchant
                var notif = new Notification(
                    transaction.UserId,
                    title: "Kích hoạt hiển thị nổi bật thành công",
                    content: $"Sản phẩm '{product.Name}' của bạn đã được kích hoạt gói đẩy Top {planName} thành công. Thời hạn hiển thị đến: {untilFormatted}.",
                    type: NotificationType.System,
                    actionUrl: $"/products/{product.Id}"
                );
                _context.Notifications.Add(notif);

                // Gửi Email hóa đơn & xác nhận đẩy Top qua Brevo SMTP
                var user = transaction.User ?? await _context.Users.FirstOrDefaultAsync(u => u.Id == transaction.UserId, cancellationToken);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    try
                    {
                        var emailBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Kích hoạt hiển thị nổi bật thành công</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #0f172a;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; padding: 40px 16px;'>
        <tr>
            <td align='center'>
                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 520px; background-color: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;'>
                    <tr>
                        <td align='center' style='padding: 36px 32px 12px 32px;'>
                            <div style='font-size: 20px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px;'>
                                🎁 GiftFinder For Business
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td align='center' style='padding: 0 32px;'>
                            <h1 style='font-size: 22px; font-weight: 700; color: #0f172a; margin: 12px 0 8px 0; line-height: 1.3;'>
                                Kích hoạt gói đẩy Top thành công! 🚀
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 420px;'>
                                Chào {user.FullName}, giao dịch thanh toán qua VNPAY cho sản phẩm của bạn đã hoàn tất.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 0 32px 28px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <tr>
                                    <td style='padding: 22px 24px;'>
                                        <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Sản phẩm:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 14px; font-weight: 700; color: #0f172a;'>{product.Name}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Gói tài trợ:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 14px; font-weight: 700; color: #2563eb;'>{planName}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Số tiền thanh toán:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 14px; font-weight: 700; color: #16a34a;'>{transaction.Amount:N0}đ</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Cổng thanh toán:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 13px; font-weight: 600; color: #0f172a;'>VNPAY Gateway</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Hiệu lực đến:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 13px; font-weight: 700; color: #0f172a;'>{untilFormatted}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 12px; color: #94a3b8;'>Mã đơn VNPAY:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 12px; color: #94a3b8; font-family: monospace;'>{gatewayTxnId}</td>
                                            </tr>
                                        </table>
                                        <div style='margin-top: 18px; padding-top: 16px; border-top: 1px solid #e2e8f0;'>
                                            <div style='font-size: 12px; font-weight: 700; color: #0f172a; margin-bottom: 8px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                                Đặc quyền hiển thị của bạn
                                            </div>
                                            <div style='font-size: 13px; color: #475569; line-height: 1.6;'>
                                                <div style='margin-bottom: 6px;'>✓ Ưu tiên xuất hiện tại các vị trí vàng (#1, #3, #7) theo thuật toán Interleaved Slotting</div>
                                                <div style='margin-bottom: 6px;'>✓ Gắn nhãn độc quyền 'Top Pick • Đối tác' hoặc 'Được tài trợ'</div>
                                                <div>✓ Tăng 300% lượt tiếp cận khách hàng tìm quà tặng thông qua AI</div>
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/merchant/dashboard' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Xem thống kê gian hàng &rarr;
                            </a>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Cảm ơn bạn đã lựa chọn GiftFinder để quảng bá quà tặng tới hàng ngàn khách hàng tiềm năng.
                            </p>
                            <p style='font-size: 12px; color: #cbd5e1; margin: 0;'>
                                &copy; {DateTime.UtcNow.Year} GiftFinder Platform. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
                        await _emailService.SendEmailAsync(user.Email, $"[GiftFinder] Xác nhận kích hoạt gói Featured Placement: {product.Name}", emailBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi gửi email xác nhận Featured cho Merchant: {Email}", user.Email);
                    }
                }
            }
        }
        else
        {
            // =========================================================================
            // NHÁNH A: GÓI PREMIUM SUBSCRIPTION CHO USER (UC-14, UC-24)
            // =========================================================================
            int months = 1;
            if (transaction.Amount >= 150_000) months = 12;
            else if (transaction.Amount >= 45_000) months = 3;

            var user = transaction.User;
            if (user != null)
            {
                user.UpgradeToPremium(months, autoRenew: true);

                // Tự động mở khóa các sản phẩm theo dõi giá đang bị tạm khóa (nếu có)
                var lockedTrackings = await _context.PriceTrackings
                    .Where(pt => pt.UserId == user.Id && pt.IsLocked)
                    .ToListAsync(cancellationToken);

                foreach (var pt in lockedTrackings)
                {
                    pt.UnlockAfterPremiumRenewal();
                }

                // Ghi nhận bản ghi thông báo hệ thống
                var notification = new Notification(
                    user.Id,
                    title: "Nâng cấp Premium thành công",
                    content: "Chúc mừng, tài khoản của bạn đã được nâng cấp lên Premium qua VNPAY.",
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
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Chúc mừng thành viên Premium</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #0f172a;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; padding: 40px 16px;'>
        <tr>
            <td align='center'>
                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 520px; background-color: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;'>
                    <tr>
                        <td align='center' style='padding: 36px 32px 12px 32px;'>
                            <div style='font-size: 20px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px;'>
                                🎁 GiftFinder
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td align='center' style='padding: 0 32px;'>
                            <h1 style='font-size: 24px; font-weight: 700; color: #0f172a; margin: 12px 0 8px 0; line-height: 1.3;'>
                                Chúc mừng thành viên Premium! 🎉
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 420px;'>
                                Chào {user.FullName}, giao dịch thanh toán qua cổng VNPAY đã thành công. Toàn bộ đặc quyền thành viên VIP của bạn đã được kích hoạt.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 0 32px 28px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <tr>
                                    <td style='padding: 22px 24px;'>
                                        <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Gói thành viên:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 14px; font-weight: 700; color: #0f172a;'>Premium VIP ({months} tháng)</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Số tiền thanh toán:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 14px; font-weight: 700; color: #16a34a;'>{transaction.Amount:N0}đ</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Cổng thanh toán:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 13px; font-weight: 600; color: #0f172a;'>VNPAY Gateway</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 13px; color: #64748b;'>Thời hạn sử dụng:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 13px; font-weight: 600; color: #0f172a;'>Đến {expiresDateFormatted}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 6px 0; font-size: 12px; color: #94a3b8;'>Mã đơn VNPAY:</td>
                                                <td align='right' style='padding: 6px 0; font-size: 12px; color: #94a3b8; font-family: monospace;'>{gatewayTxnId}</td>
                                            </tr>
                                        </table>
                                        <div style='margin-top: 18px; padding-top: 16px; border-top: 1px solid #e2e8f0;'>
                                            <div style='font-size: 12px; font-weight: 700; color: #0f172a; margin-bottom: 10px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                                Đặc quyền Premium của bạn
                                            </div>
                                            <div style='font-size: 13px; color: #475569; line-height: 1.6;'>
                                                <div style='margin-bottom: 6px;'>✓ Tự động theo dõi biến động giá &amp; nhận thông báo giảm giá (tối đa 20 sản phẩm)</div>
                                                <div style='margin-bottom: 6px;'>✓ Nhắc lịch sinh nhật, kỷ niệm tự động không giới hạn qua Email</div>
                                                <div>✓ Trợ lý AI tư vấn quà tặng cá nhân hóa chuyên sâu theo sở thích &amp; cung hoàng đạo</div>
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/tai-khoan' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Khám phá đặc quyền Premium &rarr;
                            </a>
                        </td>
                    </tr>
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Cảm ơn bạn đã đồng hành và nâng cấp tài khoản cùng GiftFinder.
                            </p>
                            <p style='font-size: 12px; color: #cbd5e1; margin: 0;'>
                                &copy; {DateTime.UtcNow.Year} GiftFinder Platform. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
                    await _emailService.SendEmailAsync(user.Email, "🎉 Chúc mừng bạn đã nâng cấp thành công thành viên Premium!", htmlBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email xác nhận Premium qua Brevo SMTP: {Email}", user.Email);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new PaymentWebhookResultDto
        {
            Success = true,
            RspCode = "00",
            TransactionId = transaction.Id,
            Status = transaction.Status.ToString(),
            Message = "Confirm Success"
        };
    }
}
