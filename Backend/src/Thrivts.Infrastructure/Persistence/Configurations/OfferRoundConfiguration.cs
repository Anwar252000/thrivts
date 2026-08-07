using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class OfferRoundConfiguration : IEntityTypeConfiguration<OfferRound>
{
    public void Configure(EntityTypeBuilder<OfferRound> builder)
    {
        builder.ToTable("offer_rounds");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.PostedBy)
            .HasConversion<string>()
            .HasColumnName("posted_by");
    }
}
