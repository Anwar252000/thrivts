using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class TranslationEntryConfiguration : IEntityTypeConfiguration<TranslationEntry>
{
    public void Configure(EntityTypeBuilder<TranslationEntry> builder)
    {
        builder.ToTable("translations");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd(); // plain integer identity, not a uuid.

        // Language maps to the native language_pref Postgres enum via Npgsql's own enum support
        // (NpgsqlEnumMapping.Configure) — a local string HasConversion here would fight that global
        // mapping and fail with "column is of type language_pref but expression is of type text"
        // the same way Requirement.Grade did before it got HasColumnType (see that fix's history).

        builder.HasIndex(t => new { t.Key, t.Language }).IsUnique();
    }
}
