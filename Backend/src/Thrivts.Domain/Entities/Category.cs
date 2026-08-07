using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A product category, referenced by requirements.category_id and platform_fee_overrides
/// (scope='category') in the live schema. README_HANDOVER.md §4 notes the admin UI is currently
/// add-only — edit/delete is a fast-follow this migration should close.
/// </summary>
public class Category : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Category()
    {
        // EF Core
    }

    public Category(string name)
    {
        Name = name;
    }

    public void Rename(string name) => Name = name;

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
