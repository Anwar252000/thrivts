using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Anthropic;
using Npgsql;
using Thrivts.Application.Features.Owner;
using Thrivts.Infrastructure.Assistant;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Security;
using Thrivts.Infrastructure.Common;
using Thrivts.Infrastructure.Identity;
using Thrivts.Infrastructure.Persistence;
using Thrivts.Infrastructure.Seeding;

namespace Thrivts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClock, KarachiClock>();
        services.AddHttpContextAccessor();
        services.AddSingleton<ICurrentUser, CurrentUser>();
        services.AddSingleton<AuditingInterceptor>();

        // One data source for the whole process: it owns the connection pool.
        services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());

        // Pooled contexts: instances are reset and reused instead of allocated per request, which
        // cuts allocation and GC pressure under heavy traffic.
        services.AddDbContextPool<ThrivtsDbContext>((sp, options) =>
        {
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>(), npgsql =>
                {
                    npgsql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                    npgsql.CommandTimeout(30);
                    npgsql.MigrationsHistoryTable("__ef_migrations_history");
                })
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(sp.GetRequiredService<AuditingInterceptor>())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        }, poolSize: 256);

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<ThrivtsDbContext>());
        services.AddScoped<ISerialGenerator, SerialGenerator>();
        services.AddScoped<DatabaseSeeder>();

        services.AddSingleton<IPinHasher, PinHasher>();
        services.AddSingleton<ITokenService, TokenService>();

        services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 4 * 1024 * 1024;
            options.MaximumKeyLength = 256;
        });
        services.AddSingleton<ICacheService, HybridCacheService>();

        services.AddOptions<AssistantOptions>().Bind(configuration.GetSection(AssistantOptions.SectionName));
        services.AddSingleton(sp => new AnthropicClient
        {
            ApiKey = sp.GetRequiredService<IOptions<AssistantOptions>>().Value.ApiKey ?? string.Empty,
            Timeout = TimeSpan.FromSeconds(90),
            MaxRetries = 2,
        });
        services.AddSingleton<IAssistantClient, AnthropicAssistantClient>();

        AddAuthentication(services, configuration);

        return services;
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = TokenService.SigningKey(jwt.Value),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = JwtOptions.RoleClaim,
                };

                // SignalR's WebSocket transport cannot send headers, so the hub accepts the token
                // from the query string — only on the hub path.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                            context.Token = token;

                        return Task.CompletedTask;
                    },
                };
            });

        var authorization = services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        foreach (var permission in Permissions.All)
            authorization.AddPolicy(permission, policy => policy.AddRequirements(new PermissionRequirement(permission)));

        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
    }
}
