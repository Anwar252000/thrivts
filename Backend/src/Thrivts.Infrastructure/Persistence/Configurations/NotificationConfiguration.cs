using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);

        // Channel and Language map to the native notification_channel/language_pref Postgres
        // enums via Npgsql's own enum support — see NpgsqlEnumMapping.Configure.

        builder.HasIndex(n => new { n.RecipientId, n.IsRead, n.CreatedAt });

        // The live table has created_at but no updated_at.
        builder.Ignore(n => n.UpdatedAt);
    }
}
