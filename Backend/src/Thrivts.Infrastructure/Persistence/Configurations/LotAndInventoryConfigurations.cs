using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class LotConfiguration : IEntityTypeConfiguration<Lot>
{
    public void Configure(EntityTypeBuilder<Lot> b)
    {
        b.ToTable("lots");
        b.Property(x => x.Serial).HasMaxLength(20).IsRequired();
        b.Property(x => x.SupplierName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.Property(x => x.SentBackReason).HasMaxLength(500);
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Serial);
        b.HasIndex(x => new { x.Stage, x.GatedAt });
        b.HasIndex(x => x.SupplierId);
        b.HasIndex(x => x.ArrivalDate);
        b.ToTable(t => t.HasCheckConstraint("ck_lots_sacks", "sacks > 0"));
    }
}

public class LotItemConfiguration : IEntityTypeConfiguration<LotItem>
{
    public void Configure(EntityTypeBuilder<LotItem> b)
    {
        b.ToTable("lot_items");
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.HasIndex(x => new { x.LotId, x.NormalizedItemName }).IsUnique();
        b.Ignore(x => x.TotalRejected);
        b.Ignore(x => x.PendingRejections);
        b.HasOne<Item>().WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_lot_items_non_negative", "received >= 0 AND qty_a >= 0 AND qty_b >= 0 AND qty_c >= 0 AND rejected >= 0 AND wash_rejected >= 0");
            t.HasCheckConstraint("ck_lot_items_returned", "returned_rejected >= 0 AND returned_rejected <= rejected + wash_rejected");
        });
    }
}

public class GateExitConfiguration : IEntityTypeConfiguration<GateExit>
{
    public void Configure(EntityTypeBuilder<GateExit> b)
    {
        b.ToTable("gate_exits");
        b.Property(x => x.Serial).HasMaxLength(20).IsRequired();
        b.Property(x => x.SupplierName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Serial).IsUnique();
        b.HasIndex(x => x.CreatedAt);
    }
}

public class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> b)
    {
        b.ToTable("stock_balances");
        b.Ignore(x => x.IsUntouched);
        b.Property(x => x.LotSerial).HasMaxLength(20).IsRequired();
        b.Property(x => x.SupplierName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Ignore(x => x.Available);
        b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LotItem>().WithMany().HasForeignKey(x => x.LotItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.LotItemId, x.Grade }).IsUnique();
        b.HasIndex(x => new { x.NormalizedItemName, x.Grade, x.ArrivalDate });
        b.HasIndex(x => x.LotId);
        b.HasIndex(x => x.SupplierId);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_stock_balances_on_hand", "on_hand >= 0 AND on_hand <= quantity_in + wash_in");
            t.HasCheckConstraint("ck_stock_balances_in_sacks", "in_sacks >= 0 AND in_sacks <= on_hand");
            t.HasCheckConstraint("ck_stock_balances_wash", "at_wash >= 0 AND wash_in >= 0 AND wash_lost >= 0 AND quantity_in >= 0");
            // The conservation rule: every piece that ever entered this balance is somewhere.
            t.HasCheckConstraint("ck_stock_balances_conserved",
                "quantity_in + wash_in = on_hand + at_wash + dispatched_container + dispatched_small + dispatched_sku + wash_lost");
        });
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("stock_movements");
        b.Property(x => x.LotSerial).HasMaxLength(20).IsRequired();
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.ReferenceNumber).HasMaxLength(40);
        b.HasIndex(x => x.StockBalanceId);
        b.HasIndex(x => x.LotId);
        b.HasIndex(x => new { x.OccurredAt, x.Id });
        b.HasIndex(x => x.ReferenceId);
    }
}

public class DispatchConfiguration : IEntityTypeConfiguration<Dispatch>
{
    public void Configure(EntityTypeBuilder<Dispatch> b)
    {
        b.ToTable("dispatches");
        b.Property(x => x.Number).HasMaxLength(20).IsRequired();
        b.Property(x => x.ClientName).HasMaxLength(80);
        b.Property(x => x.DestinationCountry).HasMaxLength(60);
        b.Property(x => x.ContainerNumber).HasMaxLength(60);
        b.Property(x => x.TotalWeightKg).HasPrecision(12, 3);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.DispatchId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => new { x.Type, x.DispatchDate });
    }
}

public class DispatchLineConfiguration : IEntityTypeConfiguration<DispatchLine>
{
    public void Configure(EntityTypeBuilder<DispatchLine> b)
    {
        b.ToTable("dispatch_lines");
        b.Property(x => x.LotSerial).HasMaxLength(20).IsRequired();
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(300);
        b.HasOne<StockBalance>().WithMany().HasForeignKey(x => x.StockBalanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.StockBalanceId);
        b.ToTable(t => t.HasCheckConstraint("ck_dispatch_lines_quantity", "quantity > 0"));
    }
}

public class RejectionReturnConfiguration : IEntityTypeConfiguration<RejectionReturn>
{
    public void Configure(EntityTypeBuilder<RejectionReturn> b)
    {
        b.ToTable("rejection_returns");
        b.Property(x => x.SlipNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.SupplierName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.RejectionReturnId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.BusinessDate);
        b.HasIndex(x => x.SupplierId);
    }
}

public class RejectionReturnLineConfiguration : IEntityTypeConfiguration<RejectionReturnLine>
{
    public void Configure(EntityTypeBuilder<RejectionReturnLine> b)
    {
        b.ToTable("rejection_return_lines");
        b.Property(x => x.LotSerial).HasMaxLength(20).IsRequired();
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.HasOne<LotItem>().WithMany().HasForeignKey(x => x.LotItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.LotItemId);
    }
}
