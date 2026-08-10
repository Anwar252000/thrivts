using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class OpsPresenceConfiguration : IEntityTypeConfiguration<OpsPresence>
{
    public void Configure(EntityTypeBuilder<OpsPresence> builder)
    {
        builder.ToTable("ops_presence");

        builder.HasKey(p => p.Who);
        builder.Property(p => p.Who).ValueGeneratedNever();
    }
}
