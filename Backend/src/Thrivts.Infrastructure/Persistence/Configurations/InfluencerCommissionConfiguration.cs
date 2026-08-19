using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class InfluencerCommissionConfiguration : IEntityTypeConfiguration<InfluencerCommission>
{
    public void Configure(EntityTypeBuilder<InfluencerCommission> builder)
    {
        builder.ToTable("influencer_commissions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .HasConversion(new SnakeCaseEnumConverter<InfluencerCommissionStatus>());

        // The live table has created_at but no updated_at.
        builder.Ignore(c => c.UpdatedAt);
    }
}
