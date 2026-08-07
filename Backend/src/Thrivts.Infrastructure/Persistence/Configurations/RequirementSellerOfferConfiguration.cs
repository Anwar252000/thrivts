using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class RequirementSellerOfferConfiguration : IEntityTypeConfiguration<RequirementSellerOffer>
{
    public void Configure(EntityTypeBuilder<RequirementSellerOffer> builder)
    {
        builder.ToTable("requirement_seller_offers");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasColumnName("status");
    }
}
