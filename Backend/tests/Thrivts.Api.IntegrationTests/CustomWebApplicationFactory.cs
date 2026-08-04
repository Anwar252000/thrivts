using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Thrivts.Api.IntegrationTests;

/// <summary>
/// Boots the real Program.cs composition root against a throwaway Testcontainers Postgres and
/// dummy-but-well-formed Supabase settings, so DI validates exactly as it would in production.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["Supabase:Url"] = "https://test.supabase.co",
                ["Supabase:JwtSecret"] = "test-jwt-secret-at-least-32-characters-long",
                ["Resend:ApiKey"] = "test-key"
            });
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
