using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Application.Features.Merchants.DTOs;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Application.Features.Merchants.Commands.ProcessFeaturedExpiration;

public class ProcessFeaturedExpirationCommandHandler : IRequestHandler<ProcessFeaturedExpirationCommand, ProcessFeaturedExpirationResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessFeaturedExpirationCommandHandler> _logger;

    public ProcessFeaturedExpirationCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<ProcessFeaturedExpirationCommandHandler> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ProcessFeaturedExpirationResultDto> Handle(ProcessFeaturedExpirationCommand request, CancellationToken cancellationToken)
    {
        var result = new ProcessFeaturedExpirationResultDto();
        var now = DateTime.UtcNow;

        // 1. QUÉT & TẮT TRẠNG THÁI NỔI BẬT CỦA CÁC SẢN PHẨM ĐÃ HẾT HẠN (BR-27)
        var expiredProducts = await _context.Products
            .Include(p => p.Merchant)
                .ThenInclude(m => m!.User)
            .Where(p => p.IsFeatured && p.FeaturedUntil.HasValue && p.FeaturedUntil.Value <= now)
            .ToListAsync(cancellationToken);

        foreach (var product in expiredProducts)
        {
            var oldUntil = product.FeaturedUntil;
            product.DeactivateFeatured();
            result.ExpiredCount++;
            result.ExpiredProductNames.Add(product.Name);

            _logger.LogInformation("[UC-20] Sản phẩm '{Name}' (ID: {Id}) đã hết hạn Featured vào {Until}. Đã hạ cờ IsFeatured = false.",
                product.Name, product.Id, oldUntil);

            // Gửi Notification và Email cho Merchant (nếu có merchant)
            if (product.Merchant != null)
            {
                var merchantUserId = product.Merchant.UserId;
                var merchantEmail = !string.IsNullOrWhiteSpace(product.Merchant.ContactEmail)
                    ? product.Merchant.ContactEmail
                    : product.Merchant.User?.Email;
                var merchantName = !string.IsNullOrWhiteSpace(product.Merchant.ShopName)
                    ? product.Merchant.ShopName
                    : "Quý đối tác";

                var notifMsg = $"Gói hiển thị nổi bật cho sản phẩm '{product.Name}' đã hết hạn vào {oldUntil:dd/MM/yyyy HH:mm} UTC. Vui lòng gia hạn để tiếp tục duy trì vị trí ưu tiên.";
                var notification = new Notification(
                    merchantUserId,
                    title: "Gói hiển thị nổi bật đã hết hạn",
                    content: notifMsg,
                    type: NotificationType.RenewalAlert,
                    actionUrl: $"/merchant/products/{product.Id}/boost"
                );
                _context.Notifications.Add(notification);

                if (!string.IsNullOrWhiteSpace(merchantEmail))
                {
                    try
                    {
                        var emailBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Gói đẩy Top sản phẩm đã hết hạn</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #0f172a;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; padding: 40px 16px;'>
        <tr>
            <td align='center'>
                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 520px; background-color: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;'>
                    <!-- Brand Header -->
                    <tr>
                        <td align='center' style='padding: 36px 32px 12px 32px;'>
                            <div style='font-size: 20px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px;'>
                                🎁 GiftFinder
                            </div>
                        </td>
                    </tr>
                    <!-- Hero Section -->
                    <tr>
                        <td align='center' style='padding: 0 32px;'>
                            <h1 style='font-size: 24px; font-weight: 700; color: #0f172a; margin: 12px 0 8px 0; line-height: 1.3;'>
                                Gói đẩy Top đã kết thúc
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 420px;'>
                                Chào {merchantName}, gói hiển thị nổi bật cho sản phẩm của bạn đã kết thúc và chuyển về vị trí mặc định.
                            </p>
                        </td>
                    </tr>
                    <!-- Detail Card -->
                    <tr>
                        <td style='padding: 0 32px 28px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <tr>
                                    <td style='padding: 22px 24px;'>
                                        <div style='margin-bottom: 8px;'>
                                            <span style='font-size: 11px; background-color: #fee2e2; color: #dc2626; padding: 3px 10px; border-radius: 9999px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;'>
                                                Hết hạn đẩy Top
                                            </span>
                                        </div>
                                        <div style='font-size: 16px; font-weight: 700; color: #0f172a; line-height: 1.4; margin-bottom: 6px;'>
                                            {product.Name}
                                        </div>
                                        <div style='font-size: 13px; color: #64748b; margin-bottom: 12px;'>
                                            Thời điểm hết hạn: <strong style='color: #0f172a;'>{oldUntil:dd/MM/yyyy HH:mm} UTC</strong>
                                        </div>
                                        <div style='font-size: 13px; color: #64748b; line-height: 1.55;'>
                                            Hiện tại sản phẩm đã trở về thứ hạng thông thường. Hãy gia hạn để tiếp tục được thuật toán AI ưu tiên đưa lên hàng đầu khi người dùng tìm kiếm quà tặng.
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Call To Action -->
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/merchant/san-pham' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Gia hạn gói đẩy Top ngay &rarr;
                            </a>
                        </td>
                    </tr>
                    <!-- Minimalist Footer -->
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Bạn nhận được email này vì đang là Đối tác kinh doanh trên GiftFinder Merchant Platform.
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

                        await _emailService.SendEmailAsync(
                            merchantEmail,
                            $"[GiftFinder] Gói hiển thị nổi bật cho sản phẩm '{product.Name}' đã hết hạn",
                            emailBody,
                            cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[UC-20] Không thể gửi email thông báo hết hạn tới {Email}: {Msg}", merchantEmail, ex.Message);
                    }
                }
            }
        }

        // 2. CẢNH BÁO NHẮC GIA HẠN CHO MERCHANT TRƯỚC 3 NGÀY HẾT HẠN
        var warningWindowEnd = now.AddDays(3);
        var expiringProducts = await _context.Products
            .Include(p => p.Merchant)
                .ThenInclude(m => m!.User)
            .Where(p => p.IsFeatured && 
                        p.FeaturedUntil.HasValue && 
                        p.FeaturedUntil.Value > now && 
                        p.FeaturedUntil.Value <= warningWindowEnd &&
                        p.MerchantId.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var product in expiringProducts)
        {
            var merchantUserId = product.Merchant!.UserId;
            var actionUrl = $"/merchant/products/{product.Id}/boost";

            // Cơ chế chống gửi lặp: Kiểm tra xem trong 3 ngày qua đã gửi thông báo nhắc gia hạn cho sản phẩm này chưa
            var recentAlertThreshold = now.AddDays(-3);
            var alreadyAlerted = await _context.Notifications
                .AnyAsync(n => n.UserId == merchantUserId && 
                               n.Type == NotificationType.RenewalAlert &&
                               n.ActionUrl == actionUrl &&
                               n.CreatedAt >= recentAlertThreshold, cancellationToken);

            if (alreadyAlerted)
            {
                continue; // Đã nhắc trong vòng 3 ngày qua, không spam lặp
            }

            result.AlertedCount++;
            result.AlertedProductNames.Add(product.Name);

            var expireDateStr = product.FeaturedUntil!.Value.ToString("dd/MM/yyyy HH:mm");
            var alertContent = $"Gói hiển thị nổi bật cho sản phẩm '{product.Name}' sắp hết hạn vào {expireDateStr} UTC, gia hạn ngay để duy trì vị trí ưu tiên.";

            var notification = new Notification(
                merchantUserId,
                title: "Nhắc nhở gia hạn gói hiển thị nổi bật",
                content: alertContent,
                type: NotificationType.RenewalAlert,
                actionUrl: actionUrl
            );
            _context.Notifications.Add(notification);

            var merchantEmail = !string.IsNullOrWhiteSpace(product.Merchant.ContactEmail)
                ? product.Merchant.ContactEmail
                : product.Merchant.User?.Email;
            var merchantName = !string.IsNullOrWhiteSpace(product.Merchant.ShopName)
                ? product.Merchant.ShopName
                : "Quý đối tác";

            if (!string.IsNullOrWhiteSpace(merchantEmail))
            {
                try
                {
                    var emailBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Nhắc nhở gia hạn gói đẩy Top</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; color: #0f172a;'>
    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; padding: 40px 16px;'>
        <tr>
            <td align='center'>
                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='max-width: 520px; background-color: #ffffff; border-radius: 20px; overflow: hidden; box-shadow: 0 4px 20px rgba(0,0,0,0.04); border: 1px solid #e2e8f0;'>
                    <!-- Brand Header -->
                    <tr>
                        <td align='center' style='padding: 36px 32px 12px 32px;'>
                            <div style='font-size: 20px; font-weight: 800; color: #0f172a; letter-spacing: -0.5px;'>
                                🎁 GiftFinder
                            </div>
                        </td>
                    </tr>
                    <!-- Hero Section -->
                    <tr>
                        <td align='center' style='padding: 0 32px;'>
                            <h1 style='font-size: 24px; font-weight: 700; color: #0f172a; margin: 12px 0 8px 0; line-height: 1.3;'>
                                Gói đẩy Top sắp hết hạn ⏰
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 420px;'>
                                Chào {merchantName}, gói hiển thị nổi bật cho sản phẩm của bạn sắp hết hạn trong 3 ngày tới.
                            </p>
                        </td>
                    </tr>
                    <!-- Detail Card -->
                    <tr>
                        <td style='padding: 0 32px 28px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <tr>
                                    <td style='padding: 22px 24px;'>
                                        <div style='margin-bottom: 8px;'>
                                            <span style='font-size: 11px; background-color: #fef3c7; color: #b45309; padding: 3px 10px; border-radius: 9999px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;'>
                                                Sắp hết hạn
                                            </span>
                                        </div>
                                        <div style='font-size: 16px; font-weight: 700; color: #0f172a; line-height: 1.4; margin-bottom: 6px;'>
                                            {product.Name}
                                        </div>
                                        <div style='font-size: 13px; color: #64748b; margin-bottom: 12px;'>
                                            Thời hạn hiển thị đến: <strong style='color: #0f172a;'>{expireDateStr} UTC</strong>
                                        </div>
                                        <div style='font-size: 13px; color: #64748b; line-height: 1.55;'>
                                            Sau thời điểm trên, sản phẩm sẽ trở về vị trí thông thường và không còn được AI ưu tiên hàng đầu. Hãy gia hạn ngay để không bị gián đoạn tiếp cận khách hàng.
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Call To Action -->
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/merchant/san-pham' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Gia hạn vị trí Top ngay &rarr;
                            </a>
                        </td>
                    </tr>
                    <!-- Minimalist Footer -->
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Bạn nhận được email này vì đang là Đối tác kinh doanh trên GiftFinder Merchant Platform.
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

                    await _emailService.SendEmailAsync(
                        merchantEmail,
                        $"[GiftFinder] Gói hiển thị nổi bật cho sản phẩm '{product.Name}' sắp hết hạn",
                        emailBody,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[UC-20] Không thể gửi email nhắc gia hạn tới {Email}: {Msg}", merchantEmail, ex.Message);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        result.Message = $"Hoàn tất xử lý: {result.ExpiredCount} sản phẩm hết hạn đã được hạ cờ; {result.AlertedCount} sản phẩm sắp hết hạn đã được gửi cảnh báo gia hạn.";
        return result;
    }
}
