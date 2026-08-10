using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderType)
            .HasConversion(new SnakeCaseEnumConverter<MessageSenderType>());

        builder.Property(m => m.AttachmentsJson).HasColumnName("attachments").HasColumnType("jsonb");

        // The live table has created_at but no updated_at.
        builder.Ignore(m => m.UpdatedAt);
    }
}
