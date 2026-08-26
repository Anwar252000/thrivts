using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
{
    public void Configure(EntityTypeBuilder<Requirement> builder)
    {
        builder.ToTable("requirements");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.RequirementNumber)
            .IsUnique();

        // Status, BuyerCurrency, MinSellerTier, RequirementType, and Grade all map to their native
        // Postgres enums via Npgsql's own enum support — see NpgsqlEnumMapping.Configure (Grade
        // uses its own hand-written translator there, not SnakeCase).

        builder.Property(r => r.ExchangeRateSnapshotJson).HasColumnName("exchange_rate_snapshot").HasColumnType("jsonb");
    }
}
