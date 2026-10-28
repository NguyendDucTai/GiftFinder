using System;
using System.Threading;
using System.Threading.Tasks;
using GiftFinder.Application.Features.Merchants.Commands.ProcessFeaturedExpiration;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GiftFinder.Infrastructure.BackgroundJobs;

public class FeaturedExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeaturedExpirationWorker> _logger;

    public FeaturedExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<FeaturedExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[UC-20] FeaturedExpirationWorker đã khởi động.");

        // Chờ 25 giây sau khi khởi động app để đảm bảo DB và migration đã sẵn sàng
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("[UC-20] Bắt đầu chu kỳ quét tự động hết hạn Featured và cảnh báo trước 3 ngày...");
                using var scope = _scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var result = await mediator.Send(new ProcessFeaturedExpirationCommand(), stoppingToken);
                _logger.LogInformation("[UC-20] Chu kỳ quét hoàn tất: {Message}", result.Message);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "[UC-20] Lỗi xảy ra trong chu kỳ quét FeaturedExpirationWorker: {Message}", ex.Message);
            }

            // Định kỳ chạy mỗi 12 giờ một lần (hoặc có thể trigger thủ công qua API Admin)
            try
            {
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
