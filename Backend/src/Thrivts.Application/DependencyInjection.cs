using System.Reflection;
using FluentValidation;
using Mapster;
using MapsterMapper;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Thrivts.Application.Common.Behaviors;
using Thrivts.Application.Common.Services;

namespace Thrivts.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(assembly);

        var mapsterConfig = TypeAdapterConfig.GlobalSettings;
        mapsterConfig.Scan(assembly);
        services.AddSingleton(mapsterConfig);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<AcceptBidService>();

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(AuditLoggingBehavior<,>));

        return services;
    }
}
