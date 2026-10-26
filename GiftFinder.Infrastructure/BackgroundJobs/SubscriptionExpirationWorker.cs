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
<div style='font-family: Arial, sans-serif; padding: 20px; line-height: 1.6; color: #333;'>
  <div style='max-width: 600px; margin: auto; border: 1px solid #fed7aa; border-radius: 8px; overflow: hidden;'>
    <div style='background: #f97316; padding: 20px; text-align: center; color: white;'>
      <h2>⚠️ THÔNG BÁO GIA HẠN GÓI PREMIUM</h2>
    </div>
    <div style='padding: 25px;'>
      <p>Xin chào <strong>{user.FullName}</strong>,</p>
      <p style='color: #c2410c; font-weight: bold;'>{warningMsg}</p>
      <p>Thời gian ân hạn của bạn có hiệu lực đến ngày: <strong>{user.GracePeriodUntil:dd/MM/yyyy HH:mm} UTC</strong>.</p>
      <p>Sau thời gian này, tài khoản sẽ tự động chuyển về gói Miễn phí và các tiện ích nâng cao sẽ tạm thời bị gián đoạn.</p>
      <p style='margin-top: 25px; text-align: center;'>
        <a href='https://localhost:7276/swagger/index.html' style='background: #f97316; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold;'>Gia hạn gói 15.000đ ngay</a>
      </p>
    </div>
  </div>
</div>";

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
<div style='font-family: Arial, sans-serif; padding: 20px; line-height: 1.6; color: #333;'>
  <div style='max-width: 600px; margin: auto; border: 1px solid #cbd5e1; border-radius: 8px; overflow: hidden;'>
    <div style='background: #64748b; padding: 20px; text-align: center; color: white;'>
      <h2>THÔNG BÁO HẾT HẠN GÓI PREMIUM</h2>
    </div>
    <div style='padding: 25px;'>
      <p>Xin chào <strong>{user.FullName}</strong>,</p>
      <p>Gói dịch vụ Premium của bạn đã hết hạn và tài khoản hiện được chuyển về gói Thành viên Miễn phí (Free).</p>
      <p>Bạn vẫn có thể tiếp tục sử dụng tính năng tìm kiếm quà tặng cơ bản hoặc nâng cấp lại gói Premium bất cứ lúc nào chỉ với <strong>15.000đ/tháng</strong>.</p>
    </div>
  </div>
</div>";

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
