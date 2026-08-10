using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class PlatformStatsConfiguration : IEntityTypeConfiguration<PlatformStats>
{
    public void Configure(EntityTypeBuilder<PlatformStats> builder)
    {
        builder.ToTable("platform_stats");

        // Singleton row — id defaults to 1 and nothing else ever writes a second one.
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
    }
}
