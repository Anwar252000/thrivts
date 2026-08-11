using Scalar.AspNetCore;
using Serilog;
using Thrivts.Api.ExceptionHandlers;
using Thrivts.Api.Extensions;
using Thrivts.Application;
using Thrivts.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add services to the container.
// Enums serialize as their C# member name (e.g. "Confirmed"), not the default int — the frontend
// never has to hardcode a numeric mapping that silently drifts if an enum is reordered.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddThrivtsApiVersioning();
builder.Services.AddThrivtsRateLimiting();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!, name: "postgres");

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("ThrivtsApps", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Scalar UI at /scalar — the modern replacement for Swagger UI.
}

app.UseHttpsRedirection();
app.UseHsts();

app.UseCors("ThrivtsApps");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Exposed for WebApplicationFactory<Program> in Thrivts.Api.IntegrationTests.
public partial class Program
{
}
