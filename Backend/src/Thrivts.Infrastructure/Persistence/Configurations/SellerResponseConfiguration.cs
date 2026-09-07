using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class SellerResponseConfiguration : IEntityTypeConfiguration<SellerResponse>
{
    public void Configure(EntityTypeBuilder<SellerResponse> builder)
    {
        builder.ToTable("seller_responses");

        builder.HasKey(sr => sr.Id);

        // Status maps to the native seller_response_status Postgres enum via Npgsql's own enum
        // support — see NpgsqlEnumMapping.Configure.

        builder.Property(sr => sr.NegotiationState)
            .HasConversion(new SnakeCaseEnumConverter<NegotiationState>());

        builder.Property(sr => sr.LastActor)
            .HasConversion(
                v => v == null ? null : EnumSnakeCase.ToSnakeCase(v.Value.ToString()),
                v => v == null ? null : EnumSnakeCase.FromSnakeCase<NegotiationActor>(v));

        // BuyerPricePerPcUsd is derived (CurrentPriceUsd + FeePerPcAppliedUsd), never stored —
        // it is also the ONLY price field a buyer-facing DTO may project (see the "fee opacity" moat rule).
        builder.Ignore(sr => sr.BuyerPricePerPcUsd);

        // seller_responses has no created_at column — responded_at (set in the constructor) is its
        // creation timestamp instead (confirmed live: inserting BaseEntity.CreatedAt by convention
        // throws "column created_at of relation seller_responses does not exist").
        builder.Ignore(sr => sr.CreatedAt);
    }
}
