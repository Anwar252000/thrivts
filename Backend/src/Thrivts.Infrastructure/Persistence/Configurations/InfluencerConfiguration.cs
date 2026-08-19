using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class InfluencerConfiguration : IEntityTypeConfiguration<Influencer>
{
    public void Configure(EntityTypeBuilder<Influencer> builder)
    {
        builder.ToTable("influencers");

        builder.HasKey(i => i.Id);
        // Id is its own generated key here (unlike Buyer/Seller/Agency/Profile) — UserId is the
        // optional, unenforced link to a profile. See Influencer's class remarks.

        builder.HasIndex(i => i.ReferralCode)
            .IsUnique();

        builder.HasIndex(i => i.InfluencerCode)
            .IsUnique();

        // The live table has created_at but no updated_at.
        builder.Ignore(i => i.UpdatedAt);
    }
}
