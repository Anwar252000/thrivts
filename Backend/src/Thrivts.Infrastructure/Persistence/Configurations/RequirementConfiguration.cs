using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
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

        builder.Property(r => r.Status)
            .HasConversion(new SnakeCaseEnumConverter<RequirementStatus>());

        builder.Property(r => r.Grade)
            .HasConversion(new GradeTypeConverter());

        builder.Property(r => r.BuyerCurrency)
            .HasConversion<string>(); // CurrencyType member names ARE the DB labels (USD/GBP/EUR/PKR).

        builder.Property(r => r.MinSellerTier)
            .HasConversion(new SnakeCaseEnumConverter<SellerTier>());

        builder.Property(r => r.RequirementType)
            .HasConversion(
                v => v == null ? null : EnumSnakeCase.ToSnakeCase(v.Value.ToString()),
                v => v == null ? null : EnumSnakeCase.FromSnakeCase<Thrivts.Domain.Enums.RequirementType>(v));

        builder.Property(r => r.ExchangeRateSnapshotJson).HasColumnName("exchange_rate_snapshot").HasColumnType("jsonb");
    }
}
