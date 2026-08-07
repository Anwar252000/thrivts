using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// Shipping cost/pc for a destination country (the live schema's shipping_rates table, feeding
/// Deal.ShippingCostUsd — see FIX4_accept_bid_VERIFIED.sql, currently hardcoded to 0 there).
/// </summary>
public class ShippingRate : BaseEntity, IAggregateRoot
{
    public string DestinationCountry { get; private set; } = default!;
    public decimal CostPerPcUsd { get; private set; }
    public Guid? CategoryId { get; private set; }

    private ShippingRate()
    {
        // EF Core
    }

    public ShippingRate(string destinationCountry, decimal costPerPcUsd, Guid? categoryId = null)
    {
        DestinationCountry = destinationCountry;
        CostPerPcUsd = costPerPcUsd;
        CategoryId = categoryId;
    }

    public void UpdateRate(decimal costPerPcUsd) => CostPerPcUsd = costPerPcUsd;
}
