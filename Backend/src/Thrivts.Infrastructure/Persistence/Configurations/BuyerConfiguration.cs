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
    }
}
