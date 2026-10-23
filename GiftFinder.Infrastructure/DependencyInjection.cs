using GiftFinder.Application.Common.Interfaces;
using GiftFinder.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GiftFinder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Đăng ký ApplicationDbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Đăng ký Auth Services
        services.Configure<Authentication.JwtSettings>(configuration.GetSection(Authentication.JwtSettings.SectionName));
        services.AddSingleton<IJwtTokenGenerator, Authentication.JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, Authentication.PasswordHasher>();

        // Đăng ký AI Service kết nối GroqCloud LPU siêu tốc
        services.AddHttpClient<IAiRecommendationService, AI.GroqAiService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Đăng ký Email & Notification Services (UC-21)
        services.Configure<GiftFinder.Application.Common.Models.EmailSettings>(
            configuration.GetSection("EmailSettings"));
        services.AddTransient<IEmailService, Services.SmtpEmailService>();
        services.AddScoped<IReminderNotificationService, Services.ReminderNotificationService>();
        services.AddHostedService<BackgroundJobs.BirthdayReminderWorker>();

        // Đăng ký Web Scraper Service (Playwright Headless Browser ngầm)
        services.AddSingleton<IWebScraperService, Scraping.PlaywrightScraperService>();

        // Đăng ký Affiliate Network Service (Shopee & TikTok Shop qua Accesstrade)
        services.AddHttpClient<IAffiliateNetworkService, Affiliate.AccesstradeService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        // Đăng ký Payment Service (VietQR / PayOS) & Subscription Expiration Worker (UC-14, UC-24, BR-20)
        services.AddScoped<IPaymentService, Services.VietQrPaymentService>();
        services.AddHostedService<BackgroundJobs.SubscriptionExpirationWorker>();

        // [UC-20] Đăng ký Featured Expiration Worker tự động hạ cờ gói đẩy Top và nhắc gia hạn trước 3 ngày
        services.AddHostedService<BackgroundJobs.FeaturedExpirationWorker>();

        return services;
    }
}
