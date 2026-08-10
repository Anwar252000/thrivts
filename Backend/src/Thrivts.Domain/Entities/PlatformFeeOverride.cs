using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A per-category or per-seller override of the global platform fee (the live schema's
/// platform_fee_overrides table — effective_fee_per_pc() resolves seller override > category
/// override > global default). NOTE: category_id here is typed uuid in the live DB even though
/// categories.id is an integer — that mismatch is in the source schema, not introduced here;
/// verify before relying on it.
/// </summary>
public class PlatformFeeOverride : IAggregateRoot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Scope { get; private set; } = default!;
    public Guid? CategoryId { get; private set; }
    public Guid? SellerId { get; private set; }
    public decimal FeePerPcUsd { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PlatformFeeOverride()
    {
        // EF Core
    }

    public PlatformFeeOverride(string scope, decimal feePerPcUsd, Guid? categoryId = null, Guid? sellerId = null)
    {
        Scope = scope;
        FeePerPcUsd = feePerPcUsd;
        CategoryId = categoryId;
        SellerId = sellerId;
    }
}
