using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class AgencyConfiguration : IEntityTypeConfiguration<Agency>
{
    public void Configure(EntityTypeBuilder<Agency> builder)
    {
        builder.ToTable("agencies");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever(); // Id == auth.uid().

        builder.HasIndex(a => a.AgencyCode)
            .IsUnique();

        // Agency.Id is also a foreign key to profiles.id (agencies_id_fkey, live schema) — see
        // BuyerConfiguration's identical fix for why this must be declared even with no navigation.
        builder.HasOne<Profile>().WithOne().HasForeignKey<Agency>(a => a.Id);
    }
}
