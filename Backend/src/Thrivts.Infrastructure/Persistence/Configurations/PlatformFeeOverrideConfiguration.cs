using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class PlatformFeeOverrideConfiguration : IEntityTypeConfiguration<PlatformFeeOverride>
{
    public void Configure(EntityTypeBuilder<PlatformFeeOverride> builder)
    {
        builder.ToTable("platform_fee_overrides");

        builder.HasKey(o => o.Id);
    }
}
