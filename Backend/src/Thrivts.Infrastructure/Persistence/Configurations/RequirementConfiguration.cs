using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
{
    public void Configure(EntityTypeBuilder<Requirement> builder)
    {
        builder.ToTable("requirements");

        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.RequirementNumber)
            .IsUnique();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasColumnName("status");
    }
}
