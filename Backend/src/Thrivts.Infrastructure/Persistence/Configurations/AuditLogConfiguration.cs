using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        // Singular in the live schema, unlike the pluralized DbSet/property name.
        builder.ToTable("audit_log");

        builder.HasKey(a => a.Id);

        // ActorRole maps to the native user_role Postgres enum via Npgsql's own enum support —
        // see NpgsqlEnumMapping.Configure.

        builder.Property(a => a.DetailsJson).HasColumnName("details").HasColumnType("jsonb");

        // The live table has created_at but no updated_at (audit entries are append-only, never
        // modified) — BaseEntity always declares both, so this must be ignored per entity that
        // maps to a table missing the column.
        builder.Ignore(a => a.UpdatedAt);
    }
}
