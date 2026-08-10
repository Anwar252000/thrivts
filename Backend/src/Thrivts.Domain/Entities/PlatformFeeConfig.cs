using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// The single global platform fee row (the live schema's platform_fee_config table — a
/// singleton enforced by a `boolean primary key default true` check constraint in Postgres).
/// This is what SellerResponse.FeePerPcAppliedUsd freezes from at bid time.
/// </summary>
public class PlatformFeeConfig : IAggregateRoot
{
    /// <summary>Always true — the DB's `id boolean primary key check (id)` only allows one row.</summary>
    public bool Id { get; private set; } = true;
    public decimal FeePerPcUsd { get; private set; } = 0.70m;
    public string PkrReference { get; private set; } = "≈ PKR 200";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public Guid? UpdatedBy { get; private set; }

    private PlatformFeeConfig()
    {
        // EF Core
    }

    public PlatformFeeConfig(decimal feePerPcUsd, string pkrReference)
    {
        FeePerPcUsd = feePerPcUsd;
        PkrReference = pkrReference;
    }

    public void UpdateRate(decimal feePerPcUsd, string pkrReference, Guid updatedBy, DateTimeOffset occurredAt)
    {
        FeePerPcUsd = feePerPcUsd;
        PkrReference = pkrReference;
        UpdatedBy = updatedBy;
        UpdatedAt = occurredAt;
    }
}
