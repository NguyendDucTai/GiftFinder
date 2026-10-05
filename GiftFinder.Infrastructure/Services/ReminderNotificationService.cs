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
            ? $"<p style='color: #7c3aed; font-weight: 600;'>✨ Cung hoàng đạo người nhận: {reminder.RecipientZodiac.Value.GetVietnameseName()}</p>"
            : string.Empty;

        var noteInfo = !string.IsNullOrWhiteSpace(reminder.Note)
            ? $"<p style='background: #f3f4f6; padding: 12px; border-left: 4px solid #f59e0b; border-radius: 4px; font-style: italic; color: #4b5563;'>Ghi chú: {reminder.Note}</p>"
            : string.Empty;

        var relationInfo = !string.IsNullOrWhiteSpace(reminder.RecipientRelation)
            ? $" ({reminder.RecipientRelation})"
            : string.Empty;

        return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Nhắc lịch ngày kỷ niệm</title>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f9fafb; margin: 0; padding: 24px; color: #1f2937;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1); border: 1px solid #e5e7eb;'>
        <!-- Header -->
        <div style='background: linear-gradient(135deg, #ec4899 0%, #8b5cf6 100%); padding: 32px 24px; text-align: center; color: #ffffff;'>
            <h1 style='margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px;'>🎁 GiftFinder</h1>
            <p style='margin: 8px 0 0; opacity: 0.9; font-size: 15px;'>Trợ lý gợi ý quà tặng thông minh của bạn</p>
        </div>

        <!-- Body -->
        <div style='padding: 32px 24px;'>
            <p style='font-size: 16px; margin-top: 0;'>Chào <strong>{reminder.User.FullName}</strong>,</p>
            <p style='font-size: 15px; line-height: 1.6; color: #374151;'>
                Hệ thống GiftFinder xin nhắc bạn một sự kiện quan trọng sắp diễn ra:
            </p>

            <div style='background-color: #fdf2f8; border: 1px solid #fbcfe8; border-radius: 8px; padding: 18px; margin: 20px 0; text-align: center;'>
                <div style='font-size: 14px; text-transform: uppercase; color: #be185d; font-weight: 700; letter-spacing: 1px;'>Sự kiện sắp tới</div>
                <div style='font-size: 22px; font-weight: 800; color: #9d174d; margin: 8px 0;'>{reminder.Title}{relationInfo}</div>
                <div style='font-size: 15px; color: #831843;'>
                    Chỉ còn <strong>{daysRemaining} ngày nữa</strong> (Ngày diễn ra: <strong>{reminder.EventDate:dd/MM}</strong>)
                </div>
            </div>

            {zodiacInfo}
            {noteInfo}

            <p style='font-size: 15px; line-height: 1.6; color: #374151; margin-top: 24px;'>
                Đừng để sát ngày mới chuẩn bị! Hãy để AI của GiftFinder giúp bạn chọn món quà thật bất ngờ và ý nghĩa ngay hôm nay.
            </p>

            <div style='text-align: center; margin: 32px 0;'>
                <a href='http://localhost:3000/recommendations' style='display: inline-block; background: linear-gradient(135deg, #ec4899 0%, #8b5cf6 100%); color: #ffffff; text-decoration: none; padding: 14px 28px; font-weight: 600; border-radius: 8px; font-size: 16px; box-shadow: 0 4px 6px -1px rgba(236, 72, 153, 0.3);'>
                    🔍 Xem Gợi Ý Quà Tặng Phù Hợp
                </a>
            </div>
        </div>

        <!-- Footer -->
        <div style='background-color: #f9fafb; padding: 20px 24px; text-align: center; font-size: 12px; color: #6b7280; border-top: 1px solid #e5e7eb;'>
            <p style='margin: 0;'>Bạn nhận được email này vì đã đăng ký thông báo nhắc lịch trên ứng dụng <strong>GiftFinder</strong>.</p>
            <p style='margin: 6px 0 0;'>© 2026 GiftFinder Platform. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
";
    }
}
