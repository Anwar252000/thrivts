using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Identity;

namespace Thrivts.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("users");
        b.Property(x => x.DisplayName).HasMaxLength(AppUser.MaxDisplayNameLength).IsRequired();
        b.Property(x => x.Title).HasMaxLength(60).IsRequired();
        b.Property(x => x.Color).HasMaxLength(9).IsRequired();
        b.Property(x => x.PinHash).HasMaxLength(200).IsRequired();
        b.HasIndex(x => new { x.App, x.IsActive, x.SortOrder });
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedByIp).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(300);
        b.Property(x => x.RevokedReason).HasMaxLength(100);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.FamilyId);
        b.HasIndex(x => x.ExpiresAt);
        b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("suppliers");
        b.Property(x => x.Name).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedName).HasMaxLength(Supplier.MaxNameLength).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.City).HasMaxLength(60);
        b.Property(x => x.Since).HasMaxLength(40);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasIndex(x => x.NormalizedName).IsUnique();
    }
}

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> b)
    {
        b.ToTable("items");
        b.Property(x => x.Name).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Category).HasMaxLength(60);
        b.Property(x => x.SkuCategory).HasMaxLength(40);
        b.Property(x => x.GradeNote).HasMaxLength(100);
        b.Property(x => x.WeightKg).HasPrecision(10, 3);
        b.Property(x => x.RejectRule).HasMaxLength(2000);
        b.OwnsOne(x => x.Criteria, c => c.ToJson());
        b.HasIndex(x => x.NormalizedName).IsUnique();
        b.HasIndex(x => x.Category);
    }
}

public class ItemPriceConfiguration : IEntityTypeConfiguration<ItemPrice>
{
    public void Configure(EntityTypeBuilder<ItemPrice> b)
    {
        b.ToTable("item_prices");
        b.HasIndex(x => x.ItemId).IsUnique();
        b.HasOne<Item>().WithOne().HasForeignKey<ItemPrice>(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class LabourerConfiguration : IEntityTypeConfiguration<Labourer>
{
    public void Configure(EntityTypeBuilder<Labourer> b)
    {
        b.ToTable("labourers");
        b.Property(x => x.Name).HasMaxLength(40).IsRequired();
        b.Property(x => x.NormalizedName).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.NormalizedName).IsUnique();
    }
}

public class SkuCategoryConfiguration : IEntityTypeConfiguration<SkuCategory>
{
    public void Configure(EntityTypeBuilder<SkuCategory> b)
    {
        b.ToTable("sku_categories");
        b.Property(x => x.Name).HasMaxLength(40).IsRequired();
        b.Property(x => x.NormalizedName).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.NormalizedName).IsUnique();
        b.ToTable(t => t.HasCheckConstraint("ck_sku_categories_size", "sack_size BETWEEN 1 AND 1000"));
    }
}

public class ItemRequestConfiguration : IEntityTypeConfiguration<ItemRequest>
{
    public void Configure(EntityTypeBuilder<ItemRequest> b)
    {
        b.ToTable("item_requests");
        b.Property(x => x.ProposedName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.NormalizedName).HasMaxLength(Item.MaxNameLength).IsRequired();
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.RequestedByName).HasMaxLength(AppUser.MaxDisplayNameLength).IsRequired();
        b.Property(x => x.DecisionNote).HasMaxLength(500);
        b.HasIndex(x => new { x.Status, x.CreatedAt });
        b.HasIndex(x => x.NormalizedName).IsUnique().HasFilter("status = 'Pending'");
    }
}
