using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Infrastructure.Auth;
using Thrivts.Infrastructure.Email;
using Thrivts.Infrastructure.Persistence;
using Thrivts.Infrastructure.Services;

namespace Thrivts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ThrivtsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ThrivtsDbContext>());

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddTransient<IClaimsTransformation, ProfileRoleClaimsTransformation>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.AddHttpClient<IEmailSender, ResendEmailSender>()
            .AddStandardResilienceHandler();

        services.Configure<SupabaseAuthOptions>(configuration.GetSection(SupabaseAuthOptions.SectionName));
        services.AddHttpClient<ISupabaseAuthClient, SupabaseAuthClient>()
            .AddStandardResilienceHandler();

        AddSupabaseAuthentication(services, configuration);

        return services;
    }

    private static void AddSupabaseAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var supabaseUrl = configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url is not configured.");
        var jwtSecret = configuration["Supabase:JwtSecret"]
            ?? throw new InvalidOperationException("Supabase:JwtSecret is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{supabaseUrl}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
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
