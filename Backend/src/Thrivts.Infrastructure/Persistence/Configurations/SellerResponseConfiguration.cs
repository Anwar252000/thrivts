using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class SellerResponseConfiguration : IEntityTypeConfiguration<SellerResponse>
{
    public void Configure(EntityTypeBuilder<SellerResponse> builder)
    {
        builder.ToTable("seller_responses");

        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.Status)
            .HasConversion<string>()
            .HasColumnName("status");

        builder.Property(sr => sr.NegotiationState)
            .HasConversion<string>()
            .HasColumnName("negotiation_state");

        builder.Property(sr => sr.LastActor)
            .HasConversion<string>()
            .HasColumnName("last_actor");

        // BuyerPricePerPcUsd is derived (CurrentPriceUsd + FeePerPcAppliedUsd), never stored —
        // it is also the ONLY price field a buyer-facing DTO may project (see the "fee opacity" moat rule).
        builder.Ignore(sr => sr.BuyerPricePerPcUsd);
    }
}
