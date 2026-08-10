using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class StockoutEntryConfiguration : IEntityTypeConfiguration<StockoutEntry>
{
    public void Configure(EntityTypeBuilder<StockoutEntry> builder)
    {
        builder.ToTable("stockout_entries");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.DataJson).HasColumnName("data").HasColumnType("jsonb");
    }
}
