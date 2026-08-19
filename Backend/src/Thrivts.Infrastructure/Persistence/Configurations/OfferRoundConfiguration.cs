using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class OfferRoundConfiguration : IEntityTypeConfiguration<OfferRound>
{
    public void Configure(EntityTypeBuilder<OfferRound> builder)
    {
        builder.ToTable("offer_rounds");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Party)
            .HasConversion(new SnakeCaseEnumConverter<NegotiationActor>())
            .HasColumnName("party");

        // The live table has created_at but no updated_at.
        builder.Ignore(o => o.UpdatedAt);
    }
}
