using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class OpsTaskConfiguration : IEntityTypeConfiguration<OpsTask>
{
    public void Configure(EntityTypeBuilder<OpsTask> builder)
    {
        builder.ToTable("ops_tasks");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever(); // arbitrary text key, not a uuid.

        builder.Property(t => t.Week).HasColumnName("w");
        builder.Property(t => t.Category).HasColumnName("cat");
        builder.Property(t => t.Owner).HasColumnName("own");
        builder.Property(t => t.Priority).HasColumnName("p");
    }
}
