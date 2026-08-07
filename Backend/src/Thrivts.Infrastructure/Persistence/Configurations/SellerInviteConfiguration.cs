using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class SellerInviteConfiguration : IEntityTypeConfiguration<SellerInvite>
{
    public void Configure(EntityTypeBuilder<SellerInvite> builder)
    {
        builder.ToTable("seller_invites");

        builder.HasKey(i => i.Id);

        builder.HasIndex(i => i.Token)
            .IsUnique();
    }
}
