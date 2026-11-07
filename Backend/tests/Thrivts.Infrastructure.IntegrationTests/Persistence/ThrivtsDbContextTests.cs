using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Identity;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;
using Thrivts.Infrastructure.Persistence;

namespace Thrivts.Infrastructure.IntegrationTests.Persistence;

/// <summary>
/// Runs against a real Postgres in a Testcontainers container (not an in-memory fake), applying
/// the real migrations, so constraints, indexes and Npgsql behaviour are exercised for real.
/// Requires Docker to be running locally / in CI.
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
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Enums_are_stored_as_text_and_filterable()
    {
        _db.Users.Add(new AppUser("Ali", "Gate Guard", UserRole.GateGuard, "#0EA5E9", "hash", 0));
        await _db.SaveChangesAsync();

        var role = UserRole.GateGuard;
        var reloaded = await _db.Users.AsNoTracking().SingleAsync(u => u.Role == role);

        reloaded.App.Should().Be(AppKind.Staff);
    }

    [Fact]
    public async Task Stock_can_never_go_negative_even_if_code_tries()
    {
        var supplier = new Supplier("Khurram");
        _db.Suppliers.Add(supplier);
        var lot = new Lot("VEWL01", supplier.Id, supplier.Name, 3, new DateOnly(2026, 11, 3), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var line = lot.AddItem(null, "TNF Fleece Regular", 10);
        _db.Lots.Add(lot);
        var balance = new StockBalance(lot.Id, line.Id, null, lot.Serial, supplier.Id, supplier.Name, lot.ArrivalDate, line.ItemName, Grade.A, 10, DateTimeOffset.UtcNow);
        _db.StockBalances.Add(balance);
        await _db.SaveChangesAsync();

        var act = () => _db.StockBalances.Where(b => b.Id == balance.Id).ExecuteUpdateAsync(s => s.SetProperty(b => b.OnHand, b => b.OnHand - 11));

        await act.Should().ThrowAsync<Npgsql.PostgresException>().Where(e => e.SqlState == "23514");
    }
}
