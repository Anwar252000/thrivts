using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Orders;
using Thrivts.Domain.Sku;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class SackConfiguration : IEntityTypeConfiguration<Sack>
{
    public void Configure(EntityTypeBuilder<Sack> b)
    {
        b.ToTable("sacks");
        b.Property(x => x.Code).HasMaxLength(20).IsRequired();
        b.Property(x => x.Prefix).HasMaxLength(SkuPrefix.MaxLength).IsRequired();
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.SkuCategory).HasMaxLength(40).IsRequired();
        b.Property(x => x.OrderName).HasMaxLength(80);
        b.Property(x => x.CriteriaNote).HasMaxLength(300);
        b.Ignore(x => x.Pieces);
        b.Ignore(x => x.IsOpened);
        b.Ignore(x => x.IsAssigned);
        b.Ignore(x => x.SpaceLeft);
        b.HasMany(x => x.Components).WithOne().HasForeignKey(x => x.SackId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Components).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.Status, x.NormalizedItemName });
        b.HasIndex(x => new { x.Status, x.Destination });
        b.HasIndex(x => x.OrderLineId);
        b.ToTable(t => t.HasCheckConstraint("ck_sacks_quantities", "qty_a >= 0 AND qty_c >= 0"));
    }
}

public class SackComponentConfiguration : IEntityTypeConfiguration<SackComponent>
{
    public void Configure(EntityTypeBuilder<SackComponent> b)
    {
        b.ToTable("sack_components");
        b.Property(x => x.LotSerial).HasMaxLength(20).IsRequired();
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.HasOne<StockBalance>().WithMany().HasForeignKey(x => x.StockBalanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.StockBalanceId);
        b.ToTable(t => t.HasCheckConstraint("ck_sack_components_quantity", "quantity >= 0"));
    }
}

public class SkuPrefixConfiguration : IEntityTypeConfiguration<SkuPrefix>
{
    public void Configure(EntityTypeBuilder<SkuPrefix> b)
    {
        b.ToTable("sku_prefixes");
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Prefix).HasMaxLength(SkuPrefix.MaxLength).IsRequired();
        b.HasIndex(x => x.NormalizedItemName).IsUnique();
        b.HasIndex(x => x.Prefix).IsUnique();
    }
}

public class SkuDispatchConfiguration : IEntityTypeConfiguration<SkuDispatch>
{
    public void Configure(EntityTypeBuilder<SkuDispatch> b)
    {
        b.ToTable("sku_dispatches");
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(120);
        b.Property(x => x.OrderName).HasMaxLength(80);
        b.Property(x => x.SackCode).HasMaxLength(20);
        b.HasMany(x => x.Components).WithOne().HasForeignKey(x => x.SkuDispatchId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Components).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => new { x.OccurredAt, x.Id });
    }
}

public class SkuDispatchComponentConfiguration : IEntityTypeConfiguration<SkuDispatchComponent>
{
    public void Configure(EntityTypeBuilder<SkuDispatchComponent> b)
    {
        b.ToTable("sku_dispatch_components");
        b.Property(x => x.LotSerial).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.StockBalanceId);
    }
}

public class ClientOrderConfiguration : IEntityTypeConfiguration<ClientOrder>
{
    public void Configure(EntityTypeBuilder<ClientOrder> b)
    {
        b.ToTable("client_orders");
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.NormalizedName).HasMaxLength(80).IsRequired();
        b.Property(x => x.ClientName).HasMaxLength(80);
        b.Property(x => x.LoginCode).HasMaxLength(40);
        b.Property(x => x.DestinationCountry).HasMaxLength(60);
        b.Property(x => x.WeightKg).HasPrecision(12, 3);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.ClientOrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Payments).WithOne().HasForeignKey(x => x.ClientOrderId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.Payments).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasIndex(x => x.NormalizedName).IsUnique().HasFilter("is_deleted = false");
        b.HasIndex(x => x.LoginCode).IsUnique().HasFilter("login_code IS NOT NULL AND is_deleted = false");
        b.HasIndex(x => new { x.IsCompleted, x.Deadline });
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> b)
    {
        b.ToTable("order_lines");
        b.Property(x => x.ItemName).HasMaxLength(120).IsRequired();
        b.Property(x => x.NormalizedItemName).HasMaxLength(120).IsRequired();
        b.Property(x => x.Grade).HasMaxLength(20);
        b.Property(x => x.Category).HasMaxLength(60);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Ignore(x => x.Remaining);
        b.HasIndex(x => new { x.ClientOrderId, x.NormalizedItemName }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_order_lines_quantities", "required >= 0 AND ready >= 0 AND client_ready >= 0"));
    }
}

public class ClientOrderPaymentConfiguration : IEntityTypeConfiguration<ClientOrderPayment>
{
    public void Configure(EntityTypeBuilder<ClientOrderPayment> b)
    {
        b.ToTable("client_order_payments");
        b.Property(x => x.Label).HasMaxLength(120);
        b.HasIndex(x => x.ClientOrderId);
    }
}
