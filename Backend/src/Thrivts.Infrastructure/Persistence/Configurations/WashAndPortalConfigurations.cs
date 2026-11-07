using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;
using Thrivts.Domain.Orders;
using Thrivts.Domain.Wash;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class WashVendorConfiguration : IEntityTypeConfiguration<WashVendor>
{
    public void Configure(EntityTypeBuilder<WashVendor> b)
    {
        b.ToTable("wash_vendors");
        b.Property(x => x.Name).HasMaxLength(WashVendor.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedName).HasMaxLength(WashVendor.MaxNameLength).IsRequired();
        b.HasIndex(x => x.NormalizedName).IsUnique();
        b.HasMany(x => x.Rates).WithOne().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Rates).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class WashRateConfiguration : IEntityTypeConfiguration<WashRate>
{
    public void Configure(EntityTypeBuilder<WashRate> b)
    {
        b.ToTable("wash_rates");
        b.Property(x => x.Category).HasMaxLength(30).IsRequired();
        b.HasIndex(x => new { x.VendorId, x.Category }).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_wash_rates_rate", "rate_per_piece >= 0"));
    }
}

public class WashItemCategoryConfiguration : IEntityTypeConfiguration<WashItemCategory>
{
    public void Configure(EntityTypeBuilder<WashItemCategory> b)
    {
        b.ToTable("wash_item_categories");
        b.Property(x => x.NormalizedItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Category).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.NormalizedItemName).IsUnique();
    }
}

public class WashBatchConfiguration : IEntityTypeConfiguration<WashBatch>
{
    public void Configure(EntityTypeBuilder<WashBatch> b)
    {
        b.ToTable("wash_batches");
        b.Property(x => x.Number).HasMaxLength(30).IsRequired();
        b.Property(x => x.LotSerial).HasMaxLength(20).IsRequired();
        b.Property(x => x.VendorName).HasMaxLength(WashVendor.MaxNameLength).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Ignore(x => x.SentPieces);
        b.Ignore(x => x.StillOut);
        b.Ignore(x => x.AmountDue);
        b.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.WashBatchId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.HasOne<Lot>().WithMany().HasForeignKey(x => x.LotId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<WashVendor>().WithMany().HasForeignKey(x => x.VendorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.Number).IsUnique();
        b.HasIndex(x => x.LotId);
        b.HasIndex(x => new { x.Status, x.SentDate });
        b.HasIndex(x => new { x.CreatedAt, x.Id });
    }
}

public class WashLineConfiguration : IEntityTypeConfiguration<WashLine>
{
    public void Configure(EntityTypeBuilder<WashLine> b)
    {
        b.ToTable("wash_lines");
        b.Property(x => x.ItemName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Category).HasMaxLength(30).IsRequired();
        b.Ignore(x => x.StillOut);
        b.Ignore(x => x.Good);
        b.Ignore(x => x.AmountDue);
        b.HasOne<StockBalance>().WithMany().HasForeignKey(x => x.StockBalanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<LotItem>().WithMany().HasForeignKey(x => x.LotItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.StockBalanceId);
        b.HasIndex(x => x.LotItemId);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_wash_lines_sent", "sent > 0");
            t.HasCheckConstraint("ck_wash_lines_back", "back_a >= 0 AND back_c >= 0 AND rejected >= 0 AND back_a + back_c + rejected <= sent");
            t.HasCheckConstraint("ck_wash_lines_price", "price_per_piece >= 0");
        });
    }
}

public class ClientMessageConfiguration : IEntityTypeConfiguration<ClientMessage>
{
    public void Configure(EntityTypeBuilder<ClientMessage> b)
    {
        b.ToTable("client_messages");
        b.Property(x => x.OrderName).HasMaxLength(80).IsRequired();
        b.Property(x => x.ClientName).HasMaxLength(80);
        b.Property(x => x.Message).HasMaxLength(ClientMessage.MaxMessageLength).IsRequired();
        b.Property(x => x.RequestedWhen).HasMaxLength(80);
        b.HasOne<ClientOrder>().WithMany().HasForeignKey(x => x.ClientOrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.IsDone, x.CreatedAt });
        b.HasIndex(x => x.ClientOrderId);
    }
}
