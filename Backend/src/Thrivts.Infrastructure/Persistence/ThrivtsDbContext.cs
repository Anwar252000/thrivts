using System.Linq.Expressions;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Finance;
using Thrivts.Domain.Identity;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;
using Thrivts.Domain.Operations;
using Thrivts.Domain.Orders;
using Thrivts.Domain.Sku;
using Thrivts.Domain.Wash;

namespace Thrivts.Infrastructure.Persistence;

public class ThrivtsDbContext(DbContextOptions<ThrivtsDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemPrice> ItemPrices => Set<ItemPrice>();
    public DbSet<Labourer> Labourers => Set<Labourer>();
    public DbSet<SkuCategory> SkuCategories => Set<SkuCategory>();
    public DbSet<ItemRequest> ItemRequests => Set<ItemRequest>();

    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<LotItem> LotItems => Set<LotItem>();
    public DbSet<GateExit> GateExits => Set<GateExit>();

    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Dispatch> Dispatches => Set<Dispatch>();
    public DbSet<DispatchLine> DispatchLines => Set<DispatchLine>();
    public DbSet<RejectionReturn> RejectionReturns => Set<RejectionReturn>();

    public DbSet<Sack> Sacks => Set<Sack>();
    public DbSet<SackComponent> SackComponents => Set<SackComponent>();
    public DbSet<SkuPrefix> SkuPrefixes => Set<SkuPrefix>();
    public DbSet<SkuDispatch> SkuDispatches => Set<SkuDispatch>();

    public DbSet<ClientOrder> ClientOrders => Set<ClientOrder>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<ClientOrderPayment> ClientOrderPayments => Set<ClientOrderPayment>();
    public DbSet<ClientMessage> ClientMessages => Set<ClientMessage>();

    public DbSet<WashVendor> WashVendors => Set<WashVendor>();
    public DbSet<WashItemCategory> WashItemCategories => Set<WashItemCategory>();
    public DbSet<WashBatch> WashBatches => Set<WashBatch>();
    public DbSet<WashLine> WashLines => Set<WashLine>();

    public DbSet<DailySubmission> DailySubmissions => Set<DailySubmission>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Blocker> Blockers => Set<Blocker>();
    public DbSet<DaySummary> DaySummaries => Set<DaySummary>();
    public DbSet<TeamCheck> TeamChecks => Set<TeamCheck>();
    public DbSet<ZoneCheck> ZoneChecks => Set<ZoneCheck>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    public DbSet<Escalation> Escalations => Set<Escalation>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    public DbSet<VendorInvoice> VendorInvoices => Set<VendorInvoice>();
    public DbSet<VendorPayment> VendorPayments => Set<VendorPayment>();

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<SerialCounter> SerialCounters => Set<SerialCounter>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public async Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        var strategy = Database.CreateExecutionStrategy();
        var attempt = 0;

        return await strategy.ExecuteAsync(async ct =>
        {
            if (attempt++ > 0)
                ChangeTracker.Clear();

            await using var transaction = await Database.BeginTransactionAsync(ct);
            var result = await work(ct);

            // A handler that returns an error (rather than throwing) may already have written some
            // rows — never commit half a unit of work.
            if (result is IErrorOr { IsError: true })
            {
                await transaction.RollbackAsync(ct);
                ChangeTracker.Clear();
                return result;
            }

            await transaction.CommitAsync(ct);
            return result;
        }, cancellationToken);
    }

    public void ClearTracked() => ChangeTracker.Clear();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ThrivtsDbContext).Assembly);
        ApplyCrossCuttingConventions(modelBuilder);
    }

    /// <summary>Enums are stored as readable text (not ints, not native PG enums) so the database
    /// stays self-describing and new values never need an ALTER TYPE; money defaults to
    /// numeric(18,2); soft-deleted rows are hidden from every query; versioned rows use xmin.</summary>
    private static void ApplyCrossCuttingConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(e => !e.IsOwned()))
        {
            var clrType = entityType.ClrType;

            // Ids are minted in the domain (UUIDv7), never by the database. Without this EF assumes
            // a set Guid key means "already saved", so a new child added through a navigation
            // (a lot's items, an order's lines) would be sent as an UPDATE and fail as a conflict.
            if (typeof(Entity).IsAssignableFrom(clrType) && entityType.BaseType is null)
                entityType.FindProperty(nameof(Entity.Id))!.ValueGenerated = ValueGenerated.Never;

            foreach (var property in entityType.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;

                if (type.IsEnum && property.GetValueConverter() is null)
                {
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(type);
                    property.SetValueConverter((ValueConverter)Activator.CreateInstance(converterType)!);
                    property.SetMaxLength(40);
                }
                else if (type == typeof(decimal) && property.GetPrecision() is null)
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var parameter = Expression.Parameter(clrType, "e");
                var filter = Expression.Lambda(
                    Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted))),
                    parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
            }

            if (typeof(IVersioned).IsAssignableFrom(clrType))
                modelBuilder.Entity(clrType).Property(nameof(IVersioned.Version)).IsRowVersion();
        }
    }
}
