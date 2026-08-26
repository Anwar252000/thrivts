using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class BuyerConfiguration : IEntityTypeConfiguration<Buyer>
{
    public void Configure(EntityTypeBuilder<Buyer> builder)
    {
        builder.ToTable("buyers");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever(); // Id == auth.uid().

        builder.Property(b => b.SocialMediaJson).HasColumnName("social_media").HasColumnType("jsonb");
        builder.Property(b => b.CategoriesJson).HasColumnName("categories").HasColumnType("jsonb");

        // Live column is "typical_req_type", not EF's default snake_case guess for the property name.
        builder.Property(b => b.TypicalRequirementType).HasColumnName("typical_req_type");

        // Buyer.Id is also a foreign key to profiles.id (buyers_id_fkey, live schema) — declaring
        // it (no navigation property either side; these stay independent aggregates) lets EF's
        // change-tracker insert the Profile row before the Buyer row when both are new in the same
        // SaveChanges call. Without this, EF has no dependency edge between the two entities and
        // can order the inserts either way, occasionally violating the FK.
        builder.HasOne<Profile>().WithOne().HasForeignKey<Buyer>(b => b.Id);
    }
}
