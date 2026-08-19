using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Infrastructure.Auth;
using Thrivts.Infrastructure.Configuration;
using Thrivts.Infrastructure.Email;
using Thrivts.Infrastructure.Persistence;
using Thrivts.Infrastructure.Services;

namespace Thrivts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Built exactly once and shared as a singleton. Passing a fresh NpgsqlDataSourceBuilder
        // (or fresh MapEnum translator instances) into UseNpgsql on every DbContext construction
        // makes EF Core treat each context's configuration as distinct, which forces a new
        // internal service provider per DbContext and eventually throws
        // ManyServiceProvidersCreatedWarning — see NpgsqlEnumMapping's doc comment.
        services.AddSingleton(_ =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("Default"));
            NpgsqlEnumMapping.ConfigureDataSource(dataSourceBuilder);
            return dataSourceBuilder.Build();
        });

        services.AddDbContext<ThrivtsDbContext>((sp, options) =>
            options.UseNpgsql(
                    sp.GetRequiredService<NpgsqlDataSource>(),
                    npgsqlOptions =>
                    {
                        // Supavisor's transaction-mode pooler (port 6543) can hand a logical
                        // "connection" a different physical backend per statement, so Npgsql's
                        // automatic prepared-statement cache (keyed to one backend) goes stale and
                        // surfaces as random stream-read timeouts — EnableRetryOnFailure absorbs
                        // those transient blips instead of bubbling a 500 up to the caller.
                        npgsqlOptions.EnableRetryOnFailure();

                        // Registers every native-Postgres-enum-backed property with Npgsql's own
                        // enum support so WHERE-clause parameters are sent pre-typed as the real
                        // enum, not text — see NpgsqlEnumMapping's own comment for why this is
                        // required (HasColumnType alone does not fix it).
                        NpgsqlEnumMapping.ConfigureContextOptions(npgsqlOptions);
                    })
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ThrivtsDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddTransient<IClaimsTransformation, ProfileRoleClaimsTransformation>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IAppUrlProvider, AppUrlProvider>();

        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.AddHttpClient<IEmailSender, ResendEmailSender>()
            .AddStandardResilienceHandler();

        services.Configure<SupabaseAuthOptions>(configuration.GetSection(SupabaseAuthOptions.SectionName));
        services.AddHttpClient<ISupabaseAuthClient, SupabaseAuthClient>()
            .AddStandardResilienceHandler();
        services.AddHttpClient<ISupabaseAdminClient, SupabaseAdminClient>()
            .AddStandardResilienceHandler();

        AddSupabaseAuthentication(services, configuration);

        return services;
    }

    private static void AddSupabaseAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Supabase's current projects sign access tokens with an asymmetric key (ES256,
                // rotatable) rather than the legacy shared HS256 secret — there is no static
                // "secret" to configure at all. Authority makes the handler fetch
                // {Authority}/.well-known/openid-configuration -> jwks_uri, cache the public
                // keys, resolve by the token's `kid`, and refresh automatically on rotation.
                options.Authority = $"{supabaseUrl}/auth/v1";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{supabaseUrl}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("AdminOnly", policy => policy.RequireClaim(ProfileRoleClaimsTransformation.RoleClaimType, "Admin"))
            .AddPolicy("BuyerOnly", policy => policy.RequireClaim(ProfileRoleClaimsTransformation.RoleClaimType, "Buyer"))
            .AddPolicy("SellerOnly", policy => policy.RequireClaim(ProfileRoleClaimsTransformation.RoleClaimType, "Seller"));
    }
}
