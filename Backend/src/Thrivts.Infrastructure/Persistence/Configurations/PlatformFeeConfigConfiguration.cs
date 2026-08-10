using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class PlatformFeeConfigConfiguration : IEntityTypeConfiguration<PlatformFeeConfig>
{
    public void Configure(EntityTypeBuilder<PlatformFeeConfig> builder)
    {
        builder.ToTable("platform_fee_config");

        // Singleton row — the live DB enforces this with `id boolean primary key check (id)`.
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
    }
}
