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

        // Đăng ký AI Service
        services.AddHttpClient<IAiRecommendationService, AI.GeminiAiService>();

        return services;
    }
}
