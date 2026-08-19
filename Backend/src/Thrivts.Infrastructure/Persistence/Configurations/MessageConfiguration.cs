using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(m => m.Id);

        // SenderType maps to the native message_sender_type Postgres enum via Npgsql's own enum
        // support — see NpgsqlEnumMapping.Configure.

        builder.Property(m => m.AttachmentsJson).HasColumnName("attachments").HasColumnType("jsonb");

        // The live table has created_at but no updated_at.
        builder.Ignore(m => m.UpdatedAt);
    }
}
