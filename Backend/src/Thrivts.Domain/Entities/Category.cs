using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A product category. NOTE: unlike most entities here, the live schema's categories.id is a
/// plain integer identity column, not a uuid — so this does NOT inherit BaseEntity.
/// Referenced by requirements.category_id and platform_fee_overrides (scope='category').
/// README_HANDOVER.md §4 notes the admin UI is currently add-only — edit/delete is a fast-follow.
/// </summary>
public class Category : IAggregateRoot
{
    public int Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? NameFr { get; private set; }
    public decimal? WeightPerPieceKg { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Category()
    {
        // EF Core
    }

    public Category(string name, string? nameFr = null, decimal? weightPerPieceKg = null, int displayOrder = 0)
    {
        Name = name;
        NameFr = nameFr;
        WeightPerPieceKg = weightPerPieceKg;
        DisplayOrder = displayOrder;
    }

    public void Rename(string name, string? nameFr)
    {
        Name = name;
        NameFr = nameFr;
    }

    public void SetDisplayOrder(int displayOrder) => DisplayOrder = displayOrder;

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
