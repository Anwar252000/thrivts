using Microsoft.EntityFrameworkCore;
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

namespace Thrivts.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<AppUser> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Supplier> Suppliers { get; }
    DbSet<Item> Items { get; }
    DbSet<ItemPrice> ItemPrices { get; }
    DbSet<Labourer> Labourers { get; }
    DbSet<SkuCategory> SkuCategories { get; }
    DbSet<ItemRequest> ItemRequests { get; }

    DbSet<Lot> Lots { get; }
    DbSet<LotItem> LotItems { get; }
    DbSet<GateExit> GateExits { get; }

    DbSet<StockBalance> StockBalances { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<Dispatch> Dispatches { get; }
    DbSet<DispatchLine> DispatchLines { get; }
    DbSet<RejectionReturn> RejectionReturns { get; }

    DbSet<Sack> Sacks { get; }
    DbSet<SackComponent> SackComponents { get; }
    DbSet<SkuPrefix> SkuPrefixes { get; }
    DbSet<SkuDispatch> SkuDispatches { get; }

    DbSet<ClientOrder> ClientOrders { get; }
    DbSet<OrderLine> OrderLines { get; }
    DbSet<ClientOrderPayment> ClientOrderPayments { get; }
    DbSet<ClientMessage> ClientMessages { get; }

    DbSet<WashVendor> WashVendors { get; }
    DbSet<WashItemCategory> WashItemCategories { get; }
    DbSet<WashBatch> WashBatches { get; }
    DbSet<WashLine> WashLines { get; }

    DbSet<DailySubmission> DailySubmissions { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Blocker> Blockers { get; }
    DbSet<DaySummary> DaySummaries { get; }
    DbSet<TeamCheck> TeamChecks { get; }
    DbSet<ZoneCheck> ZoneChecks { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }

    DbSet<Escalation> Escalations { get; }
    DbSet<Approval> Approvals { get; }
    DbSet<Goal> Goals { get; }
    DbSet<ActivityEvent> ActivityEvents { get; }

    DbSet<VendorInvoice> VendorInvoices { get; }
    DbSet<VendorPayment> VendorPayments { get; }

    DbSet<StoredFile> StoredFiles { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Runs the work in one database transaction, wrapped in the connection's retry
    /// strategy so a transient network blip retries the whole unit instead of leaving it half
    /// applied. Use for anything that moves stock or money across several rows.</summary>
    Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);

    /// <summary>Discards tracked changes — used before retrying a unit of work.</summary>
    void ClearTracked();
}
