using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class DealAllocationConfiguration : IEntityTypeConfiguration<DealAllocation>
{
    public void Configure(EntityTypeBuilder<DealAllocation> builder)
    {
        builder.ToTable("deal_allocations");

        builder.HasKey(a => a.Id);
    }
}
