using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> builder)
    {
        builder.ToTable("sellers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever(); // Id == auth.uid().

        // Tier maps to the native seller_tier Postgres enum via Npgsql's own enum support — see
        // NpgsqlEnumMapping.Configure.

        builder.Property(s => s.SocialMediaJson).HasColumnName("social_media").HasColumnType("jsonb");

        // "WhatsApp" splits as "whats_app" under the snake_case naming convention, but the real
        // column is the single token "whatsapp" (it's a brand name, not two words).
        builder.Property(s => s.WhatsApp).HasColumnName("whatsapp");

        builder.HasIndex(s => s.PublicAlias)
            .IsUnique(); // the buyer-facing pseudonym must never collide — see README_HANDOVER.md's "moat".

        // Seller.Id is also a foreign key to profiles.id (sellers_id_fkey, live schema) — see
        // BuyerConfiguration's identical fix for why this must be declared even with no navigation.
        builder.HasOne<Profile>().WithOne().HasForeignKey<Seller>(s => s.Id);
    }
}
