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

        builder.HasIndex(s => s.PublicAlias)
            .IsUnique(); // the buyer-facing pseudonym must never collide — see README_HANDOVER.md's "moat".
    }
}
