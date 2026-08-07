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

        builder.Property(i => i.Id)
            .ValueGeneratedNever(); // Id == auth.uid().

        builder.HasIndex(i => i.ReferralCode)
            .IsUnique();
    }
}
