using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class WashSentEntryConfiguration : IEntityTypeConfiguration<WashSentEntry>
{
    public void Configure(EntityTypeBuilder<WashSentEntry> builder)
    {
        builder.ToTable("wash_sent");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.DataJson).HasColumnName("data").HasColumnType("jsonb");
    }
}
