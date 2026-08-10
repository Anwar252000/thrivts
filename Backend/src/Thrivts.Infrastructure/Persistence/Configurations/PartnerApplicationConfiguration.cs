using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class PartnerApplicationConfiguration : IEntityTypeConfiguration<PartnerApplication>
{
    public void Configure(EntityTypeBuilder<PartnerApplication> builder)
    {
        builder.ToTable("partner_applications");

        builder.HasKey(a => a.Id);
    }
}
