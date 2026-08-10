using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// Shipping cost for a destination (feeding Deal.ShippingCostUsd). NOTE: like Category, the live
/// schema's shipping_rates.id is a plain integer identity column, not a uuid.
/// rate_usd_per_kg/rate_per_kg and destination_country/countries+zone_name look like two
/// generations of the same idea (per-destination vs. per-zone) coexisting — both kept for
/// round-trip fidelity.
/// </summary>
public class ShippingRate : IAggregateRoot
{
    public int Id { get; private set; }
    public string DestinationCountry { get; private set; } = default!;
    public string? DestinationPort { get; private set; }
    public string? ContainerSize { get; private set; }
    public decimal? RateUsdPerKg { get; private set; }
    public decimal? FlatRateUsd { get; private set; }
    public int? TransitDays { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateOnly EffectiveFrom { get; private set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateOnly? EffectiveTo { get; private set; }
    public string? Notes { get; private set; }

    public string? ZoneName { get; private set; }
    public string[]? Countries { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal? RatePerKg { get; private set; }
    public int? DeliveryDaysMin { get; private set; }
    public int? DeliveryDaysMax { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; private set; }

    private ShippingRate()
    {
        // EF Core
    }

    public ShippingRate(string destinationCountry, decimal? rateUsdPerKg = null, decimal? flatRateUsd = null,
        int? transitDays = null)
    {
        DestinationCountry = destinationCountry;
        RateUsdPerKg = rateUsdPerKg;
        FlatRateUsd = flatRateUsd;
        TransitDays = transitDays;
    }

    public void UpdateRate(decimal? rateUsdPerKg, decimal? flatRateUsd) => (RateUsdPerKg, FlatRateUsd) = (rateUsdPerKg, flatRateUsd);

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
