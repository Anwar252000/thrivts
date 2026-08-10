using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Entities;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(c => c.Id);

        // The live schema's categories.id is a plain integer identity column (nextval sequence),
        // not a client-generated uuid — the only entity here where that's true besides
        // ExchangeRate/ShippingRate.
        builder.Property(c => c.Id).ValueGeneratedOnAdd();
    }
}
