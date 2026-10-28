using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Common;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.Services;

public class ReminderNotificationService : IReminderNotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ReminderNotificationService> _logger;

    public ReminderNotificationService(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<ReminderNotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ProcessUpcomingRemindersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu tiến trình quét các ngày kỷ niệm/sinh nhật sắp tới...");

        var activeReminders = await _context.ReminderDates
            .Include(r => r.User)
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;
        var notifiedCount = 0;

        foreach (var reminder in activeReminders)
        {
            if (reminder.User == null || string.IsNullOrWhiteSpace(reminder.User.Email))
                continue;

            // Tính toán ngày kỷ niệm cho năm hiện tại
            var currentYear = today.Year;
            DateTime targetDateThisYear;

            try
            {
                targetDateThisYear = new DateTime(currentYear, reminder.EventDate.Month, reminder.EventDate.Day);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Xử lý năm nhuận (29/02)
                targetDateThisYear = new DateTime(currentYear, 2, 28);
            }

            // Nếu ngày trong năm nay đã qua, lấy ngày của năm tiếp theo
            if (targetDateThisYear < today)
            {
                try
                {
                    targetDateThisYear = new DateTime(currentYear + 1, reminder.EventDate.Month, reminder.EventDate.Day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    targetDateThisYear = new DateTime(currentYear + 1, 2, 28);
                }
            }

            var daysRemaining = (targetDateThisYear - today).Days;

            // Kiểm tra điều kiện nhắc:
            // 1. Số ngày còn lại nằm trong khoảng nhắc (<= DaysBeforeNotify và >= 0)
            // 2. Chưa từng nhắc trong vòng 30 ngày qua (tránh gửi nhiều lần cho cùng 1 dịp)
            var isWithinNoticePeriod = daysRemaining <= reminder.DaysBeforeNotify && daysRemaining >= 0;
            var notNotifiedRecently = !reminder.LastNotifiedAt.HasValue ||
                                      (today - reminder.LastNotifiedAt.Value.Date).TotalDays > 30;

            if (isWithinNoticePeriod && notNotifiedRecently)
            {
                _logger.LogInformation("Phát hiện sự kiện cần nhắc: {Title} của User {UserEmail} (còn {Days} ngày).",
                    reminder.Title, reminder.User.Email, daysRemaining);

                var subject = $"[GiftFinder] Nhắc lịch: Còn {daysRemaining} ngày nữa là đến {reminder.Title}!";
                var htmlBody = GenerateEmailTemplate(reminder, daysRemaining);

                // Gửi Email thông báo
                await _emailService.SendEmailAsync(reminder.User.Email, subject, htmlBody, cancellationToken);

                // Tạo thông báo trong hệ thống (In-app Notification)
                var inAppNotification = new Notification(
                    reminder.UserId,
                    $"Nhắc lịch: {reminder.Title}",
                    $"Còn {daysRemaining} ngày nữa là đến {reminder.Title} ({reminder.EventDate:dd/MM}). Hãy chuẩn bị quà ý nghĩa nhé!",
                    NotificationType.Reminder,
                    actionUrl: "/recommendations"
                );
                _context.Notifications.Add(inAppNotification);

                // Đánh dấu thời điểm đã thông báo
                reminder.MarkNotified(DateTime.UtcNow);
                notifiedCount++;
            }
        }

        if (notifiedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã hoàn tất gửi {Count} thông báo nhắc lịch kỷ niệm.", notifiedCount);
        }
        else
        {
            _logger.LogInformation("Không có sự kiện kỷ niệm nào cần nhắc nhở trong hôm nay.");
        }
    }

    private static string GenerateEmailTemplate(ReminderDate reminder, int daysRemaining)
    {
        var zodiacInfo = reminder.RecipientZodiac.HasValue
            ? $"<div style='font-size: 13px; color: #6366f1; font-weight: 600; margin-top: 8px;'>✨ Cung hoàng đạo: {reminder.RecipientZodiac.Value.GetVietnameseName()}</div>"
            : string.Empty;

        var noteInfo = !string.IsNullOrWhiteSpace(reminder.Note)
            ? $"<div style='font-size: 13px; color: #64748b; background-color: #ffffff; padding: 10px 14px; border-radius: 10px; border: 1px solid #e2e8f0; margin-top: 10px; font-style: italic;'>Ghi chú: {reminder.Note}</div>"
            : string.Empty;

        var relationInfo = !string.IsNullOrWhiteSpace(reminder.RecipientRelation)
            ? $" ({reminder.RecipientRelation})"
            : string.Empty;

        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Nhắc lịch ngày kỷ niệm</title>
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
                                Nhắc lịch ngày kỷ niệm sắp tới
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 420px;'>
                                Chào {reminder.User.FullName}, ngày đặc biệt của người thân yêu đang đến rất gần. Hãy để GiftFinder giúp bạn chuẩn bị món quà chu đáo nhất.
                            </p>
                        </td>
                    </tr>
                    <!-- Event Card -->
                    <tr>
                        <td style='padding: 0 32px 28px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <tr>
                                    <td style='padding: 22px 24px;'>
                                        <div style='margin-bottom: 8px;'>
                                            <span style='font-size: 11px; background-color: #eff6ff; color: #2563eb; padding: 3px 10px; border-radius: 9999px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;'>
                                                Còn {daysRemaining} ngày nữa
                                            </span>
                                        </div>
                                        <div style='font-size: 18px; font-weight: 700; color: #0f172a; line-height: 1.4; margin-bottom: 6px;'>
                                            {reminder.Title}{relationInfo}
                                        </div>
                                        <div style='font-size: 13px; color: #64748b;'>
                                            Ngày diễn ra: <strong style='color: #0f172a;'>{reminder.EventDate:dd/MM}</strong>
                                        </div>
                                        {zodiacInfo}
                                        {noteInfo}
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Call To Action -->
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/recommendations' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Khám phá quà tặng phù hợp &rarr;
                            </a>
                        </td>
                    </tr>
                    <!-- Minimalist Footer -->
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Bạn nhận được email này vì đã cài đặt lịch nhắc nhở kỷ niệm trên GiftFinder.
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
