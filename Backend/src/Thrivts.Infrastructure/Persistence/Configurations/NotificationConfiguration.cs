using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Infrastructure.Persistence.Conversions;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Channel)
            .HasConversion(new SnakeCaseEnumConverter<NotificationChannel>());

        builder.Property(n => n.Language)
            .HasConversion(new SnakeCaseEnumConverter<LanguagePref>());

        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });
    }
}
