using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("disputes");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Status)
            .HasConversion(new SnakeCaseEnumConverter<DisputeStatus>());

        builder.Property(d => d.AttachmentsJson).HasColumnName("attachments").HasColumnType("jsonb");

        builder.HasIndex(d => d.DisputeNumber).IsUnique();
    }
}
