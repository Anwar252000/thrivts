using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Identity;
using Thrivts.Domain.Sku;
using Thrivts.Domain.Wash;
using Thrivts.Infrastructure.Persistence;

namespace Thrivts.Infrastructure.Seeding;

/// <summary>
/// First-run data. Each block only runs when its table is empty, so it is safe on every start.
/// Login PINs are never in source control: initial users come from the "Bootstrap:Users" config
/// section (user-secrets / environment). The item catalogue is seeded without prices — prices are
/// owner-confidential and come from the owner app or the go-live import.
/// </summary>
public sealed class DatabaseSeeder(ThrivtsDbContext db, IPinHasher pinHasher, IConfiguration configuration, ILogger<DatabaseSeeder> logger)
{
    private static readonly string[] DefaultLabourers = ["Faraz", "Rehmat", "Haris", "Zubair", "Ghani bhai", "Zeeshan", "Husnain", "Affan"];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedUsersAsync(cancellationToken);
        await SeedSkuCategoriesAsync(cancellationToken);
        await SeedLabourersAsync(cancellationToken);
        await SeedCatalogAsync(cancellationToken);
        await SeedWashVendorsAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The two washers the legacy app hard-coded. Their rate cards start empty — the
    /// inventory manager fills them in once on the Wash tab.</summary>
    private async Task SeedWashVendorsAsync(CancellationToken ct)
    {
        if (await db.WashVendors.AnyAsync(ct))
            return;

        db.WashVendors.Add(new WashVendor("Irfan"));
        db.WashVendors.Add(new WashVendor("Kashif"));
    }

    private async Task SeedUsersAsync(CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
            return;

        var users = configuration.GetSection("Bootstrap:Users").Get<List<BootstrapUser>>() ?? [];
        if (users.Count == 0)
        {
            logger.LogWarning("No users exist and no Bootstrap:Users are configured — nobody can sign in until users are created");
            return;
        }

        var order = 0;
        foreach (var u in users)
        {
            if (!Enum.TryParse<UserRole>(u.Role, out var role))
                throw new InvalidOperationException($"Bootstrap user '{u.DisplayName}' has an unknown role '{u.Role}'.");

            var pinLength = UserRoles.PinLengthFor(role);
            if (u.Pin is null || u.Pin.Length != pinLength || !u.Pin.All(char.IsAsciiDigit))
                throw new InvalidOperationException($"Bootstrap user '{u.DisplayName}' needs a {pinLength}-digit PIN.");

            db.Users.Add(new AppUser(u.DisplayName, u.Title ?? role.ToString(), role, u.Color ?? "#1AA767", pinHasher.Hash(u.Pin), order++));
        }

        logger.LogInformation("Seeded {Count} bootstrap users", users.Count);
    }

    private async Task SeedSkuCategoriesAsync(CancellationToken ct)
    {
        if (await db.SkuCategories.AnyAsync(ct))
            return;

        foreach (var (name, size) in SkuCategoryClassifier.DefaultSackSizes)
            db.SkuCategories.Add(new SkuCategory(name, size));
    }

    private async Task SeedLabourersAsync(CancellationToken ct)
    {
        if (await db.Labourers.AnyAsync(ct))
            return;

        foreach (var name in DefaultLabourers)
            db.Labourers.Add(new Labourer(name));
    }

    private async Task SeedCatalogAsync(CancellationToken ct)
    {
        var hasSuppliers = await db.Suppliers.AnyAsync(ct);
        var hasItems = await db.Items.AnyAsync(ct);
        if (hasSuppliers && hasItems)
            return;

        await using var stream = typeof(DatabaseSeeder).Assembly.GetManifestResourceStream("Thrivts.Infrastructure.Seeding.catalog-seed.json")
            ?? throw new InvalidOperationException("catalog-seed.json is not embedded.");
        var seed = await JsonSerializer.DeserializeAsync<CatalogSeed>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web), ct)
            ?? throw new InvalidOperationException("catalog-seed.json is empty.");

        if (!hasSuppliers)
        {
            foreach (var name in seed.Suppliers.Append("Warehouse").Distinct(StringComparer.OrdinalIgnoreCase))
                db.Suppliers.Add(new Supplier(name));
        }

        if (!hasItems)
        {
            foreach (var s in seed.Items)
            {
                var item = new Item(s.Name, s.Category);
                item.UpdateDetails(s.Category, SkuCategoryClassifier.Classify(s.Name), s.Grade, s.WeightKg, NullIfEmpty(s.RejectRule));
                item.UpdateCriteria(new ItemCriteria
                {
                    Brands = NullIfEmpty(s.Criteria?.Brands),
                    Grades = NullIfEmpty(s.Criteria?.Grades),
                    RejectIf = NullIfEmpty(s.Criteria?.RejectIf),
                    Notes = NullIfEmpty(s.Criteria?.Notes),
                    Style = NullIfEmpty(s.Criteria?.Style),
                });
                db.Items.Add(item);
            }

            logger.LogInformation("Seeded {Count} catalogue items (no prices)", seed.Items.Count);
        }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record BootstrapUser(string DisplayName, string? Title, string Role, string? Color, string? Pin);

    private sealed record CatalogSeed(List<string> Suppliers, List<SeedItem> Items);

    private sealed record SeedItem(string Name, string? Category, decimal? WeightKg, string? Grade, SeedCriteria? Criteria, string? RejectRule);

    private sealed record SeedCriteria(string? Brands, string? Grades, string? RejectIf, string? Notes, string? Style);
}
