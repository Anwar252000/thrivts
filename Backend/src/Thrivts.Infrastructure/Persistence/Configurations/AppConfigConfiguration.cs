using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class AppConfigConfiguration : IEntityTypeConfiguration<AppConfig>
{
    public void Configure(EntityTypeBuilder<AppConfig> builder)
    {
        builder.ToTable("app_config");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever(); // arbitrary text key, not a uuid.

        builder.Property(c => c.DataJson).HasColumnName("data").HasColumnType("jsonb");
    }
}
