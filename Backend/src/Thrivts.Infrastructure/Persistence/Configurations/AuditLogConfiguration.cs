using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Singular in the live schema, unlike the pluralized DbSet/property name.
        builder.ToTable("audit_log");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActorRole)
            .HasConversion(
                v => v == null ? null : EnumSnakeCase.ToSnakeCase(v.Value.ToString()),
                v => v == null ? null : EnumSnakeCase.FromSnakeCase<UserRole>(v));

        builder.Property(a => a.DetailsJson).HasColumnName("details").HasColumnType("jsonb");
    }
}
