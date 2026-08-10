using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DealNumber).IsRequired();
        builder.HasIndex(d => d.DealNumber).IsUnique();

        builder.Property(d => d.Status)
            .HasConversion(new SnakeCaseEnumConverter<DealStatus>());

        builder.Property(d => d.ExchangeRateSnapshotJson).HasColumnName("exchange_rate_snapshot").HasColumnType("jsonb");
    }
}
