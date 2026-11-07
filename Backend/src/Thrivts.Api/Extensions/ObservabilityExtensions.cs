using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace Thrivts.Api.Extensions;

public static class ObservabilityExtensions
{
    /// <summary>Structured JSON-friendly logs (Serilog) enriched with machine, environment,
    /// correlation id and user; level overrides come from configuration. Health-check and
    /// static noise is dropped so the logs stay readable.</summary>
    public static WebApplicationBuilder AddThrivtsLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "thrivts-api"));

        return builder;
    }

    public static void ConfigureRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";
            options.GetLevel = (http, elapsed, ex) => ex is not null || http.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : http.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                : elapsed > 1000 ? LogEventLevel.Warning
                : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, http) =>
            {
                diagnostics.Set("UserId", http.User.FindFirst("sub")?.Value);
                diagnostics.Set("App", http.User.FindFirst("app")?.Value);
                diagnostics.Set("ClientIp", http.Connection.RemoteIpAddress?.ToString());
            };
        });
    }

    /// <summary>Distributed traces and metrics (HTTP, outgoing calls, Npgsql queries). Exported only
    /// when an OTLP endpoint is configured, so it costs nothing until a collector is wired up.</summary>
    public static IServiceCollection AddThrivtsTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var endpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        if (string.IsNullOrWhiteSpace(endpoint))
            return services;

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("thrivts-api"))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation(o => o.Filter = http => !http.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)));

        return services;
    }
}
