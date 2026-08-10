using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class WashReceivedEntryConfiguration : IEntityTypeConfiguration<WashReceivedEntry>
{
    public void Configure(EntityTypeBuilder<WashReceivedEntry> builder)
    {
        builder.ToTable("wash_received");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.DataJson).HasColumnName("data").HasColumnType("jsonb");
    }
}
