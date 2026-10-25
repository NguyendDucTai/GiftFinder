using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.Services;

public class PriceAlertService : IPriceAlertService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PriceAlertService> _logger;

    public PriceAlertService(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<PriceAlertService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<int> ProcessPriceDropsAsync(IReadOnlyList<PriceDropInfo> priceDrops, CancellationToken cancellationToken = default)
    {
        if (priceDrops == null || priceDrops.Count == 0)
            return 0;

        var productIds = priceDrops.Select(p => p.ProductId).Distinct().ToList();

        // 1. Tìm các bản ghi theo dõi giá đang hoạt động cho các sản phẩm này
        var trackings = await _context.PriceTrackings
            .Include(pt => pt.User)
            .Include(pt => pt.Product)
            .Where(pt => pt.IsActive && productIds.Contains(pt.ProductId))
            .ToListAsync(cancellationToken);

        if (trackings.Count == 0)
        {
            _logger.LogInformation("[PriceAlert] Không có người dùng nào đang theo dõi các sản phẩm giảm giá này.");
            return 0;
        }

        var userGroups = trackings.GroupBy(pt => pt.UserId);
        int totalUsersNotified = 0;
        var todayUtc = DateTime.UtcNow.Date;

        foreach (var group in userGroups)
        {
            var user = group.First().User;
            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                continue;

            // 2. Chống spam 24h (UC-22): Chỉ lấy các sản phẩm chưa được gửi thông báo trong ngày hôm nay
            var eligibleTrackings = group
                .Where(pt => !pt.LastNotifiedAt.HasValue || pt.LastNotifiedAt.Value.Date < todayUtc)
                .ToList();

            if (eligibleTrackings.Count == 0)
            {
                _logger.LogInformation("[PriceAlert] Người dùng {UserId} ({Email}) đã nhận thông báo giảm giá cho các sản phẩm này hôm nay. Bỏ qua chống spam 24h.",
                    user.Id, user.Email);
                continue;
            }

            // 3. Tạo thông báo in-app (bảng Notifications)
            var droppedItemsForEmail = new List<(string Name, string? ImageUrl, long OldPrice, long NewPrice, double DropPercent, Guid ProductId)>();

            foreach (var pt in eligibleTrackings)
            {
                var dropInfo = priceDrops.FirstOrDefault(pd => pd.ProductId == pt.ProductId);
                long newPrice = dropInfo != null ? dropInfo.NewPrice : pt.Product.Price;
                double dropPercent = dropInfo != null ? dropInfo.DropPercent : (pt.InitialPrice > 0 ? ((pt.InitialPrice - newPrice) / (double)pt.InitialPrice * 100) : 0);

                var notification = new Notification(
                    user.Id,
                    "Món quà bạn theo dõi vừa giảm giá!",
                    $"Sản phẩm '{TruncateTitle(pt.Product.Name, 45)}' vừa giảm {dropPercent:F0}% (từ {pt.InitialPrice:N0}đ xuống {newPrice:N0}đ).",
                    NotificationType.PriceDrop,
                    $"/products/{pt.ProductId}"
                );

                _context.Notifications.Add(notification);
                pt.MarkNotified(newPrice);

                droppedItemsForEmail.Add((pt.Product.Name, pt.Product.ImageUrl, pt.InitialPrice, newPrice, dropPercent, pt.ProductId));
            }

            // 4. Gửi Email thông báo tinh gọn phong cách hiện đại (như Figma, Canva, ChatGPT)
            string subject;
            string htmlBody;

            if (eligibleTrackings.Count == 1)
            {
                var item = droppedItemsForEmail[0];
                // Tiêu đề ngắn gọn, tự nhiên như Canva / Figma (chỉ 7 từ, không nhồi nhét tên sản phẩm)
                subject = "Món quà bạn quan tâm vừa hạ giá";
                htmlBody = BuildSingleItemEmailHtml(item.Name, item.ImageUrl, item.OldPrice, item.NewPrice, item.DropPercent, item.ProductId);
            }
            else
            {
                // Tiêu đề ngắn gọn, cuốn hút phong cách ChatGPT ("Có 4 phong cách ảnh mới để thử")
                subject = $"Có {eligibleTrackings.Count} món quà của bạn đang được giảm giá";
                htmlBody = BuildDigestEmailHtml(droppedItemsForEmail);
            }

            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, htmlBody, cancellationToken);
                totalUsersNotified++;
                _logger.LogInformation("[PriceAlert] Đã gửi email thông báo giảm giá ({Count} món) tới {Email}.",
                    eligibleTrackings.Count, user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PriceAlert] Lỗi khi gửi email thông báo tới {Email}: {Message}", user.Email, ex.Message);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return totalUsersNotified;
    }

    public static string CleanProductTitle(string? title, int maxLength = 48)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Món quà bạn theo dõi";
        title = title.Trim();

        // Cắt bớt từ khóa spam SEO Shopee nếu có dấu phẩy hoặc gạch ngang sớm
        int commaIndex = title.IndexOfAny(new[] { ',', '|', '-' });
        if (commaIndex > 15 && commaIndex <= maxLength)
        {
            return title.Substring(0, commaIndex).Trim();
        }

        if (title.Length <= maxLength) return title;
        return title.Substring(0, maxLength).TrimEnd() + "...";
    }

    public static string TruncateTitle(string? title, int maxLength = 48) => CleanProductTitle(title, maxLength);

    public static string BuildSingleItemEmailHtml(string fullName, string? imageUrl, long oldPrice, long newPrice, double dropPercent, Guid productId)
    {
        string displayTitle = CleanProductTitle(fullName, 52);
        string imageCell = !string.IsNullOrWhiteSpace(imageUrl)
            ? $@"<td width='116' style='padding: 16px 0 16px 16px; vertical-align: middle;'>
                    <img src='{imageUrl}' alt='{displayTitle}' width='100' height='100' style='display: block; width: 100px; height: 100px; object-fit: cover; border-radius: 12px; border: 1px solid #e2e8f0;' />
                 </td>"
            : "";

        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Món quà bạn quan tâm vừa hạ giá</title>
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
                                Món quà bạn quan tâm vừa hạ giá
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 400px;'>
                                Sản phẩm trong danh sách theo dõi của bạn vừa có đợt ưu đãi mới trên sàn đối tác.
                            </p>
                        </td>
                    </tr>
                    <!-- Product Card -->
                    <tr>
                        <td style='padding: 0 32px 28px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <tr>
                                    {imageCell}
                                    <td style='padding: 18px 20px; vertical-align: middle;'>
                                        <div style='font-size: 15px; font-weight: 600; color: #0f172a; line-height: 1.4; margin-bottom: 8px;'>
                                            {displayTitle}
                                        </div>
                                        <div style='font-size: 13px; color: #94a3b8; margin-bottom: 6px;'>
                                            Giá cũ: <span style='text-decoration: line-through;'>{oldPrice:N0}đ</span>
                                        </div>
                                        <div style='font-size: 19px; font-weight: 700; color: #0f172a;'>
                                            {newPrice:N0}đ <span style='font-size: 12px; background-color: #fee2e2; color: #dc2626; padding: 2px 8px; border-radius: 9999px; font-weight: 700; margin-left: 6px;'>-{dropPercent:F0}%</span>
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Call To Action -->
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/products/{productId}' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Xem ưu đãi ngay &rarr;
                            </a>
                        </td>
                    </tr>
                    <!-- Minimalist Footer -->
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Bạn nhận được email này vì đã bật tính năng theo dõi giá trên GiftFinder.
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
    }

    public static string BuildDigestEmailHtml(IReadOnlyList<(string Name, string? ImageUrl, long OldPrice, long NewPrice, double DropPercent, Guid ProductId)> items)
    {
        var itemsHtml = string.Join("", items.Take(4).Select(item =>
        {
            string shortTitle = CleanProductTitle(item.Name, 45);
            string imgTag = !string.IsNullOrWhiteSpace(item.ImageUrl)
                ? $"<img src='{item.ImageUrl}' alt='{shortTitle}' width='56' height='56' style='display: block; width: 56px; height: 56px; object-fit: cover; border-radius: 10px; border: 1px solid #e2e8f0;' />"
                : "<div style='width: 56px; height: 56px; background: #e2e8f0; border-radius: 10px;'></div>";

            return $@"
            <tr>
                <td style='padding: 12px 0; border-bottom: 1px solid #f1f5f9;'>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0'>
                        <tr>
                            <td width='64' style='vertical-align: middle;'>
                                {imgTag}
                            </td>
                            <td style='padding-left: 12px; vertical-align: middle;'>
                                <div style='font-size: 14px; font-weight: 600; color: #0f172a; line-height: 1.35; margin-bottom: 4px;'>{shortTitle}</div>
                                <div style='font-size: 15px; font-weight: 700; color: #0f172a;'>
                                    {item.NewPrice:N0}đ 
                                    <span style='font-size: 12px; color: #94a3b8; font-weight: normal; text-decoration: line-through; margin-left: 4px;'>{item.OldPrice:N0}đ</span>
                                    <span style='font-size: 11px; background-color: #fee2e2; color: #dc2626; padding: 2px 7px; border-radius: 9999px; font-weight: 700; margin-left: 4px;'>-{item.DropPercent:F0}%</span>
                                </div>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>";
        }));

        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Có {items.Count} món quà của bạn đang được giảm giá</title>
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
                                Có {items.Count} món quà của bạn đang giảm giá
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 24px 0; line-height: 1.55; max-width: 420px;'>
                                Các sản phẩm trong danh sách theo dõi của bạn vừa đồng loạt hạ giá hôm nay trên sàn đối tác.
                            </p>
                        </td>
                    </tr>
                    <!-- Product List -->
                    <tr>
                        <td style='padding: 0 32px 24px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #ffffff;'>
                                {itemsHtml}
                            </table>
                        </td>
                    </tr>
                    <!-- Call To Action -->
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/tai-khoan/theo-doi-gia' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Xem danh sách giảm giá &rarr;
                            </a>
                        </td>
                    </tr>
                    <!-- Minimalist Footer -->
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Bạn nhận được email này vì đã bật tính năng theo dõi giá trên GiftFinder.
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
    }
}
