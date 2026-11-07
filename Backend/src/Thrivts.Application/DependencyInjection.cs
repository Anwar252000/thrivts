using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Thrivts.Application.Common.Behaviors;
using Thrivts.Application.Common.Services;
using Thrivts.Application.Features.Inventory;

namespace Thrivts.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddScoped<StockLedger>();
        services.AddScoped<ActivityRecorder>();
        services.AddScoped<SignatureStore>();
        services.AddScoped<Features.Lots.LotEditor>();

        return services;
    }
}
