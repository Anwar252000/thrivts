using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class BlockedIpConfiguration : IEntityTypeConfiguration<BlockedIp>
{
    public void Configure(EntityTypeBuilder<BlockedIp> builder)
    {
        builder.ToTable("blocked_ips");

        builder.HasKey(b => b.Id);
    }
}
