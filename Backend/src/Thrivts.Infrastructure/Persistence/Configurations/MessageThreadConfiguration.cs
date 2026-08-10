using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class MessageThreadConfiguration : IEntityTypeConfiguration<MessageThread>
{
    public void Configure(EntityTypeBuilder<MessageThread> builder)
    {
        builder.ToTable("message_threads");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ParticipantRole)
            .HasConversion(new SnakeCaseEnumConverter<UserRole>());
    }
}
