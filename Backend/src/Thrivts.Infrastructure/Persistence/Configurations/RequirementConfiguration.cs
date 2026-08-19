using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
{
    public void Configure(EntityTypeBuilder<Requirement> builder)
    {
        builder.ToTable("requirements");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.RequirementNumber)
            .IsUnique();

        // Status, BuyerCurrency, MinSellerTier, and RequirementType map to their native Postgres
        // enums via Npgsql's own enum support — see NpgsqlEnumMapping.Configure.

        // Grade is deliberately NOT native-mapped — grade_type carries legacy-casing duplicate
        // labels (A_B, MIXED) that GradeTypeConverter's read path collapses onto AB/Mixed, which
        // Npgsql's strict one-to-one enum mapping can't express. Grade is never filtered by a
        // WHERE clause anywhere in this codebase, so the converter is safe as-is (see
        // NpgsqlEnumMapping's doc comment for the full reasoning).
        builder.Property(r => r.Grade)
            .HasConversion(new GradeTypeConverter());

        builder.Property(r => r.ExchangeRateSnapshotJson).HasColumnName("exchange_rate_snapshot").HasColumnType("jsonb");
    }
}
