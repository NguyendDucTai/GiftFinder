using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Domain.Entities;
using GiftFinder.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.BackgroundJobs;

public class SubscriptionExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpirationWorker> _logger;

    public SubscriptionExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SubscriptionExpirationWorker đã khởi động.");

        // Chờ 20 giây sau khi start app để đảm bảo DB và migration sẵn sàng
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Bắt đầu chu kỳ quét hạn gói Premium và xử lý Ân hạn (BR-20)...");

                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var now = DateTime.UtcNow;

                // 1. Quét các tài khoản Premium đã hết hạn sử dụng
                var expiredUsers = await context.Users
                    .Where(u => u.Tier == MembershipTier.Premium && 
                                u.PremiumExpiresAt.HasValue && 
                                u.PremiumExpiresAt.Value <= now)
                    .ToListAsync(stoppingToken);

                foreach (var user in expiredUsers)
                {
                    // Trường hợp A: Vừa mới hết hạn và chưa vào thời gian ân hạn -> Kích hoạt Ân hạn 3 ngày (BR-20)
                    if (!user.GracePeriodUntil.HasValue)
                    {
                        user.EnterGracePeriod();

                        var warningMsg = "Thanh toán gia hạn Premium không thành công, vui lòng cập nhật phương thức thanh toán trong vòng 3 ngày để không bị gián đoạn dịch vụ.";
                        
                        var notification = new Notification(
                            user.Id,
                            title: "Cảnh báo hết hạn gói Premium",
                            content: warningMsg,
                            type: NotificationType.RenewalAlert,
                            actionUrl: "/account/subscription"
                        );
                        context.Notifications.Add(notification);

                        try
                        {
                            var emailBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Cảnh báo hết hạn gói Premium</title>
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
                                Gói Premium sắp hết hạn ⏰
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 420px;'>
                                Chào {user.FullName}, thanh toán gia hạn của bạn chưa hoàn tất. Bạn có 3 ngày ân hạn để cập nhật thông tin và duy trì dịch vụ.
                            </p>
                        </td>
                    </tr>
                    <!-- Alert Card -->
                    <tr>
                        <td style='padding: 0 32px 28px 32px;'>
                            <table width='100%' border='0' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; border-radius: 16px; border: 1px solid #e2e8f0; overflow: hidden;'>
                                <tr>
                                    <td style='padding: 22px 24px;'>
                                        <div style='margin-bottom: 8px;'>
                                            <span style='font-size: 11px; background-color: #fef3c7; color: #b45309; padding: 3px 10px; border-radius: 9999px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;'>
                                                Thời gian ân hạn 3 ngày
                                            </span>
                                        </div>
                                        <div style='font-size: 15px; font-weight: 600; color: #0f172a; line-height: 1.4; margin-bottom: 8px;'>
                                            Thời gian ân hạn đến: <span style='color: #d97706;'>{user.GracePeriodUntil:dd/MM/yyyy HH:mm} UTC</span>
                                        </div>
                                        <div style='font-size: 13px; color: #64748b; line-height: 1.6;'>
                                            Sau thời gian này, tài khoản sẽ tự động chuyển về gói Miễn phí và danh sách sản phẩm theo dõi giá sẽ tạm khóa để bảo vệ tài nguyên máy chủ.
                                        </div>
                                        <div style='margin-top: 14px; padding-top: 12px; border-top: 1px solid #e2e8f0; font-size: 13px; color: #0f172a; font-weight: 600;'>
                                            Phí duy trì chỉ <strong>15.000đ/tháng</strong>.
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Call To Action -->
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/tai-khoan/nang-cap' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Gia hạn gói 15.000đ ngay &rarr;
                            </a>
                        </td>
                    </tr>
                    <!-- Minimalist Footer -->
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Bạn nhận được email này vì đang sử dụng gói dịch vụ Premium trên GiftFinder.
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

                            await emailService.SendEmailAsync(
                                user.Email,
                                "[GiftFinder] Cảnh báo hết hạn gói thành viên Premium - Thời gian ân hạn 3 ngày",
                                emailBody,
                                stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi gửi email cảnh báo ân hạn cho user {Email}", user.Email);
                        }

                        _logger.LogInformation("User {Email} đã chuyển sang trạng thái Ân hạn 3 ngày (Grace Period)", user.Email);
                    }
                    // Trường hợp B: Đã quá thời hạn ân hạn 3 ngày mà vẫn chưa gia hạn -> Hạ cấp về Free
                    else if (user.GracePeriodUntil.Value <= now)
                    {
                        user.DowngradeToFree();

                        // Khóa toàn bộ các sản phẩm theo dõi giá để ngừng cào giá và bảo vệ tài nguyên server
                        var userTrackings = await context.PriceTrackings
                            .Where(pt => pt.UserId == user.Id && pt.IsActive)
                            .ToListAsync(stoppingToken);

                        foreach (var pt in userTrackings)
                        {
                            pt.LockDueToPremiumExpiration();
                        }

                        var expiredMsg = "Gói Premium của bạn đã hết hạn. Danh sách sản phẩm theo dõi giá đã được tạm khóa.";
                        var notification = new Notification(
                            user.Id,
                            title: "Gói Premium đã hết hạn",
                            content: expiredMsg,
                            type: NotificationType.RenewalAlert,
                            actionUrl: "/account/subscription"
                        );
                        context.Notifications.Add(notification);

                        try
                        {
                            var emailBody = $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Thông báo hết hạn gói Premium</title>
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
                                Gói Premium đã kết thúc
                            </h1>
                            <p style='font-size: 14px; color: #64748b; margin: 0 0 28px 0; line-height: 1.55; max-width: 420px;'>
                                Chào {user.FullName}, thời gian ân hạn đã kết thúc và tài khoản của bạn hiện chuyển về gói Thành viên Miễn phí (Free).
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
                                            <span style='font-size: 11px; background-color: #f1f5f9; color: #475569; padding: 3px 10px; border-radius: 9999px; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px;'>
                                                Gói Miễn phí (Free)
                                            </span>
                                        </div>
                                        <div style='font-size: 14px; color: #0f172a; line-height: 1.6; margin-bottom: 10px;'>
                                            Danh sách sản phẩm theo dõi giá đã được tạm khóa. Dữ liệu ngày kỷ niệm và thông tin tài khoản của bạn vẫn được lưu giữ an toàn.
                                        </div>
                                        <div style='font-size: 13px; color: #64748b; line-height: 1.5;'>
                                            Bạn vẫn có thể sử dụng tính năng tìm kiếm quà tặng cơ bản hoặc nâng cấp lại gói Premium bất cứ lúc nào chỉ với <strong>15.000đ/tháng</strong> để mở khóa lại toàn bộ tính năng.
                                        </div>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                    <!-- Call To Action -->
                    <tr>
                        <td align='center' style='padding: 0 32px 36px 32px;'>
                            <a href='https://giftfinder.vn/tai-khoan/nang-cap' target='_blank' style='display: inline-block; background-color: #0f172a; color: #ffffff !important; text-decoration: none; font-size: 14px; font-weight: 600; padding: 14px 36px; border-radius: 9999px; box-shadow: 0 2px 8px rgba(15,23,42,0.15);'>
                                Nâng cấp lại Premium &rarr;
                            </a>
                        </td>
                    </tr>
                    <!-- Minimalist Footer -->
                    <tr>
                        <td style='background-color: #ffffff; padding: 20px 32px 28px 32px; border-top: 1px solid #f1f5f9; text-align: center;'>
                            <p style='font-size: 12px; color: #94a3b8; margin: 0 0 6px 0; line-height: 1.5;'>
                                Bạn nhận được email này vì đã từng đăng ký gói dịch vụ Premium trên GiftFinder.
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

                            await emailService.SendEmailAsync(
                                user.Email,
                                "[GiftFinder] Gói dịch vụ Premium của bạn đã hết hạn",
                                emailBody,
                                stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi gửi email thông báo hết hạn cho user {Email}", user.Email);
                        }

                        _logger.LogInformation("User {Email} đã hết thời gian ân hạn và được hạ cấp về gói Free", user.Email);
                    }
                }

                if (expiredUsers.Count > 0)
                {
                    await context.SaveChangesAsync(stoppingToken);
                }

                _logger.LogInformation("Hoàn tất chu kỳ kiểm tra hạn Premium. Đã xử lý {Count} tài khoản.", expiredUsers.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Lỗi không mong muốn trong SubscriptionExpirationWorker.");
            }

            // Quét định kỳ mỗi 6 tiếng
            try
            {
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SubscriptionExpirationWorker đang dừng.");
    }
}
