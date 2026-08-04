using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.ValueObjects;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DealNumber)
            .HasColumnName("deal_number")
            .IsRequired();

        builder.HasIndex(d => d.DealNumber).IsUnique();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasColumnName("status");

        // Money is a value-object struct, not an entity — EF Core 8+ complex types are the
        // correct mapping tool here (OwnsOne is for reference-type owned entities).
        builder.ComplexProperty(d => d.TotalInvoice, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_invoice_usd");
            money.Property(m => m.Currency).HasColumnName("total_invoice_currency");
        });

        builder.ComplexProperty(d => d.TotalSpread, money =>
        {
            money.Property(m => m.Amount).HasColumnName("total_spread_usd");
            money.Property(m => m.Currency).HasColumnName("total_spread_currency");
        });
    }
}
