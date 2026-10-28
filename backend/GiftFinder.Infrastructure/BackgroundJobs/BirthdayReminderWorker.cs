using GiftFinder.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.BackgroundJobs;

public class BirthdayReminderWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BirthdayReminderWorker> _logger;

    public BirthdayReminderWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BirthdayReminderWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BirthdayReminderWorker đã khởi động.");

        // Chờ 15 giây sau khi ứng dụng start để đảm bảo EF Core và Web Server đã sẵn sàng hoàn toàn
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Bắt đầu chu kỳ kiểm tra lịch sinh nhật & kỷ niệm...");
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var reminderService = scope.ServiceProvider.GetRequiredService<IReminderNotificationService>();
                    await reminderService.ProcessUpcomingRemindersAsync(stoppingToken);
                }

                _logger.LogInformation("Hoàn tất chu kỳ kiểm tra.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Lỗi không mong muốn trong tiến trình chạy ngầm BirthdayReminderWorker.");
            }

            // Tính thời gian ngủ tới 00:05 UTC ngày hôm sau (hoặc chu kỳ 24h)
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddMinutes(5);
            var delay = nextRun - now;
            if (delay <= TimeSpan.Zero)
            {
                delay = TimeSpan.FromHours(24);
            }

            _logger.LogInformation("Lần quét tiếp theo dự kiến lúc {NextRun:yyyy-MM-dd HH:mm:ss} UTC (sau {Hours:N1} giờ).",
                nextRun, delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("BirthdayReminderWorker đang dừng lại.");
    }
}
