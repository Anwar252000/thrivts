using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("commissions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasColumnName("status");
    }
}
