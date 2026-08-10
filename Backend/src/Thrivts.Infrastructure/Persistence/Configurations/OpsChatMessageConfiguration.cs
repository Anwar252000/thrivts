using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class OpsChatMessageConfiguration : IEntityTypeConfiguration<OpsChatMessage>
{
    public void Configure(EntityTypeBuilder<OpsChatMessage> builder)
    {
        builder.ToTable("ops_chat");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd(); // bigint identity column.
    }
}
