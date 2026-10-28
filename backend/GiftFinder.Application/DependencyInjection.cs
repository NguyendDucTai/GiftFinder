using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace GiftFinder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 1. Cấu hình MediatR (CQRS)
        services.AddMediatR(config => {
            config.RegisterServicesFromAssembly(assembly);
        });

        // 2. Cấu hình FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // 3. Cấu hình Mapster
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(assembly);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        return services;
    }
}
