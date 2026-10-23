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
<div style='font-family: Arial, sans-serif; padding: 20px; line-height: 1.6; color: #333;'>
  <div style='max-width: 600px; margin: auto; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
    <div style='background: #ef4444; padding: 20px; text-align: center; color: white;'>
      <h2>THÔNG BÁO HẾT HẠN GÓI HIỂN THỊ NỔI BẬT</h2>
    </div>
    <div style='padding: 25px;'>
      <p>Xin chào <strong>{merchantName}</strong>,</p>
      <p>Gói hiển thị nổi bật (Featured Placement) cho sản phẩm <strong>'{product.Name}'</strong> của bạn đã chính thức hết hạn vào ngày <strong>{oldUntil:dd/MM/yyyy HH:mm} UTC</strong>.</p>
      <p>Hiện tại sản phẩm đã trở về thứ hạng hiển thị thông thường.</p>
      <p>Để tiếp tục giữ vị trí ưu tiên hàng đầu trong các kết quả tư vấn quà tặng của AI, bạn có thể gia hạn ngay:</p>
      <p style='margin-top: 25px; text-align: center;'>
        <a href='https://localhost:7276/swagger/index.html' style='background: #ef4444; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Gia hạn gói đẩy Top ngay</a>
      </p>
    </div>
  </div>
</div>";

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
<div style='font-family: Arial, sans-serif; padding: 20px; line-height: 1.6; color: #333;'>
  <div style='max-width: 600px; margin: auto; border: 1px solid #fed7aa; border-radius: 8px; overflow: hidden;'>
    <div style='background: #f97316; padding: 20px; text-align: center; color: white;'>
      <h2>⏰ NHẮC NHỞ GIA HẠN GÓI HIỂN THỊ NỔI BẬT</h2>
    </div>
    <div style='padding: 25px;'>
      <p>Xin chào <strong>{merchantName}</strong>,</p>
      <p>{alertContent}</p>
      <p>Sau thời điểm <strong>{expireDateStr} UTC</strong>, sản phẩm sẽ tự động quay trở về thứ hạng hiển thị thông thường và không còn được thuật toán AI ưu tiên đưa lên hàng đầu.</p>
      <p style='margin-top: 25px; text-align: center;'>
        <a href='https://localhost:7276/swagger/index.html' style='background: #f97316; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Gia hạn ngay để giữ vị trí Top</a>
      </p>
    </div>
  </div>
</div>";

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
