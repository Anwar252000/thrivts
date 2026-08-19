using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class RequirementSellerOfferConfiguration : IEntityTypeConfiguration<RequirementSellerOffer>
{
    public void Configure(EntityTypeBuilder<RequirementSellerOffer> builder)
    {
        builder.ToTable("requirement_seller_offers");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .HasConversion(new SnakeCaseEnumConverter<OfferStatus>());

        builder.Property(o => o.ExchangeRateSnapshotJson).HasColumnName("exchange_rate_snapshot").HasColumnType("jsonb");

        // The live table has created_at but no updated_at.
        builder.Ignore(o => o.UpdatedAt);
    }
}
