using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thrivts.Infrastructure.Persistence;

/// <summary>
/// Design-time only factory so `dotnet ef migrations add` can build the model without booting the
/// full Thrivts.Api host (and its unrelated DI/config requirements). The connection string here is
/// never used to actually connect — `migrations add` only needs a syntactically valid Npgsql
/// options builder to generate the model snapshot. Real runtime wiring stays in
/// Thrivts.Infrastructure.DependencyInjection, driven by the Api's appsettings + user-secrets.
/// </summary>
public class ThrivtsDbContextFactory : IDesignTimeDbContextFactory<ThrivtsDbContext>
{
    public ThrivtsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ThrivtsDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=thrivts_design_time;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention();

        return new ThrivtsDbContext(optionsBuilder.Options);
    }
}
