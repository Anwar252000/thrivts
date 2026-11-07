using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;
using Thrivts.Api.Controllers.V1;
using Thrivts.Api.ExceptionHandlers;
using Thrivts.Api.Extensions;
using Thrivts.Api.Middleware;
using Thrivts.Api.Realtime;
using Thrivts.Application;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Infrastructure;
using Thrivts.Infrastructure.Persistence;
using Thrivts.Infrastructure.Seeding;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddThrivtsLogging();
    builder.WebHost.ConfigureKestrel(kestrel =>
    {
        kestrel.AddServerHeader = false;
        kestrel.Limits.MaxRequestBodySize = 2 * 1024 * 1024;
        kestrel.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
    });

    builder.Services
        .AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        });

    builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = Activity.Current?.TraceId.ToString() ?? ctx.HttpContext.TraceIdentifier;
        if (ctx.HttpContext.Items[Thrivts.Infrastructure.Identity.CurrentUser.CorrelationItemKey] is string correlationId)
            ctx.ProblemDetails.Extensions["correlationId"] = correlationId;
    });
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddThrivtsApiVersioning();
    builder.Services.AddThrivtsRateLimiting();
    builder.Services.AddThrivtsTelemetry(builder.Configuration);
    builder.Services.AddOpenApi();

    builder.Services.AddSignalR(o =>
    {
        o.MaximumReceiveMessageSize = 16 * 1024;
        o.EnableDetailedErrors = builder.Environment.IsDevelopment();
    });
    builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

    builder.Services.AddResponseCompression(o =>
    {
        o.EnableForHttps = true;
        o.Providers.Add<BrotliCompressionProvider>();
        o.Providers.Add<GzipCompressionProvider>();
        o.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/problem+json"]);
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, name: "postgres", tags: ["ready"]);

    var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy("apps", policy => policy
        .WithOrigins(corsOrigins)
        .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
        .WithHeaders("Authorization", "Content-Type", "Idempotency-Key", RequestContextMiddleware.HeaderName, "x-requested-with", "x-signalr-user-agent")
        .WithExposedHeaders(RequestContextMiddleware.HeaderName, "Retry-After")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromHours(1))));

    // Buyers' order portals: their own origins, anonymous POSTs only, never credentials.
    var portalOrigins = builder.Configuration.GetSection("Cors:PortalOrigins").Get<string[]>() ?? [];
    builder.Services.AddCors(options => options.AddPolicy(PortalCors.PolicyName, policy => policy
        .WithOrigins(portalOrigins)
        .WithMethods("POST")
        .WithHeaders("Content-Type", RequestContextMiddleware.HeaderName)
        .WithExposedHeaders(RequestContextMiddleware.HeaderName, "Retry-After")
        .SetPreflightMaxAge(TimeSpan.FromHours(1))));

    builder.Services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });

    var app = builder.Build();

    await PrepareDatabaseAsync(app);

    // Render terminates TLS at its edge and forwards plain HTTP with X-Forwarded-* headers; trust
    // them first so HTTPS redirection, rate limiting and logs all see the real scheme and client IP.
    app.UseForwardedHeaders();
    app.UseMiddleware<RequestContextMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.ConfigureRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();
    }
    else
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseResponseCompression();
    app.UseCors("apps");
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();

    app.MapControllers();
    app.MapHub<LiveHub>(LiveHub.Path);
    app.MapHealthChecks("/health/live", new() { Predicate = _ => false }).AllowAnonymous().DisableRateLimiting();
    app.MapHealthChecks("/health", new() { Predicate = c => c.Tags.Contains("ready") }).AllowAnonymous().DisableRateLimiting();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "API terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static async Task PrepareDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    if (configuration.GetValue("Database:MigrateOnStartup", app.Environment.IsDevelopment()))
        await scope.ServiceProvider.GetRequiredService<ThrivtsDbContext>().Database.MigrateAsync();

    if (configuration.GetValue("Database:SeedOnStartup", true))
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync(CancellationToken.None);
}

public partial class Program;
