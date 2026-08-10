using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class TranslationEntryConfiguration : IEntityTypeConfiguration<TranslationEntry>
{
    public void Configure(EntityTypeBuilder<TranslationEntry> builder)
    {
        builder.ToTable("translations");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd(); // plain integer identity, not a uuid.

        builder.Property(t => t.Language)
            .HasConversion(new SnakeCaseEnumConverter<LanguagePref>());

        builder.HasIndex(t => new { t.Key, t.Language }).IsUnique();
    }
}
