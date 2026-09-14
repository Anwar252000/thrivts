using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("purchase_orders");

        builder.HasKey(po => po.Id);

        builder.Property(po => po.PoNumber).IsRequired();
        builder.HasIndex(po => po.DealId).IsUnique();

        // Status maps to the native po_status Postgres enum via Npgsql's own enum support —
        // see NpgsqlEnumMapping.Configure.
    }
}
