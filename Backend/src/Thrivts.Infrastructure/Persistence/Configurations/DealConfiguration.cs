using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DealNumber).IsRequired();
        builder.HasIndex(d => d.DealNumber).IsUnique();

        // Status maps to the native deal_status Postgres enum via Npgsql's own enum support —
        // see NpgsqlEnumMapping.Configure.

        builder.Property(d => d.ExchangeRateSnapshotJson).HasColumnName("exchange_rate_snapshot").HasColumnType("jsonb");

        // The live "deals" table has cancelled_at but no cancellation_reason column — Cancel()
        // still sets the in-memory property (harmless), it just doesn't persist.
        builder.Ignore(d => d.CancellationReason);
    }
}
