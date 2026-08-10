using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Thrivts.Domain.Entities;
using Thrivts.Infrastructure.Persistence;

namespace Thrivts.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Runs against a real Postgres in a Testcontainers container (not SQLite-in-memory) — per the
/// migration architecture doc, this catches real Postgres/Npgsql behaviour that an in-memory
/// provider would hide. Requires Docker to be running locally / in CI.
/// </summary>
public class ThrivtsDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    private ThrivtsDbContext _db = default!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ThrivtsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        _db = new ThrivtsDbContext(options);
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Can_persist_and_reload_a_deal()
    {
        var deal = new Deal("DEAL-0001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), totalQuantityPcs: 100,
            buyerPricePerPcUsd: 13m, avgSellerPricePerPcUsd: 12.30m, spreadPerPcUsd: 0.70m,
            subtotalUsd: 1300m, totalInvoiceUsd: 1300m, totalSpreadUsd: 70m, totalSellerPayoutUsd: 1230m);

        _db.Deals.Add(deal);
        await _db.SaveChangesAsync(CancellationToken.None);

        var reloaded = await _db.Deals.AsNoTracking().SingleAsync(d => d.Id == deal.Id);

        reloaded.DealNumber.Should().Be("DEAL-0001");
        reloaded.TotalInvoiceUsd.Should().Be(1300m);
    }
}
