using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Common.Services;

/// <summary>
/// The "accept a bid" financial logic shared by AdminAcceptSellerResponseCommand (admin accepts on
/// the buyer's behalf) and BuyerAcceptBidCommand (buyer accepts directly) — same money model (the
/// fee IS the spread) and agency/influencer attribution either way, just reached through a
/// different authorization gate. Callers are responsible for their own role/ownership checks
/// before calling AcceptAsync.
/// </summary>
public sealed class AcceptBidService
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AcceptBidService(IApplicationDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<ErrorOr<Guid>> AcceptAsync(Guid sellerResponseId, Guid createdBy, CancellationToken cancellationToken)
    {
        var bid = await _db.SellerResponses.FirstOrDefaultAsync(sr => sr.Id == sellerResponseId, cancellationToken);
        if (bid is null)
            return Error.NotFound(description: $"Bid '{sellerResponseId}' was not found.");
        if (!bid.CanBeAccepted())
            return Error.Validation(description: "This bid can no longer be accepted.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == bid.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{bid.RequirementId}' was not found.");

        var buyer = await _db.Buyers.FirstOrDefaultAsync(b => b.Id == requirement.BuyerId, cancellationToken);

        var alreadyAccepted = await _db.SellerResponses
            .Where(sr => sr.RequirementId == bid.RequirementId && sr.Status == BidStatus.Accepted)
            .SumAsync(sr => sr.AcceptedQuantityPcs ?? sr.AvailableQuantityPcs, cancellationToken);
        var remaining = requirement.QuantityPcs - alreadyAccepted;
        if (remaining <= 0)
            return Error.Validation(description: "This requirement is already fully committed.");
        if (bid.AvailableQuantityPcs > remaining)
            return Error.Validation(description: $"This bid ({bid.AvailableQuantityPcs} pcs) exceeds the remaining quantity ({remaining} pcs).");

        var sellerPrice = bid.CurrentPriceUsd ?? bid.ProposedPriceUsd ?? 0;
        var fee = bid.FeePerPcAppliedUsd ?? 0;
        var buyerPrice = sellerPrice + fee;
        var qty = bid.AvailableQuantityPcs;
        var now = _clock.UtcNow;

        try
        {
            var dealNumber = await NextDealNumberAsync(cancellationToken);

            var deal = new Deal(
                dealNumber: dealNumber,
                requirementId: bid.RequirementId,
                buyerId: requirement.BuyerId,
                sellerId: bid.SellerId,
                totalQuantityPcs: qty,
                buyerPricePerPcUsd: buyerPrice,
                avgSellerPricePerPcUsd: sellerPrice,
                spreadPerPcUsd: fee,
                subtotalUsd: buyerPrice * qty,
                totalInvoiceUsd: buyerPrice * qty,
                totalSpreadUsd: fee * qty,
                totalSellerPayoutUsd: sellerPrice * qty,
                agencyId: buyer?.AttributedToAgency,
                sourceResponseId: bid.Id,
                createdBy: createdBy);

            deal.AdvanceTo(DealStatus.Confirmed, now);
            _db.Deals.Add(deal);

            var sellerPayout = sellerPrice * qty;
            var allocation = new DealAllocation(deal.Id, bid.SellerId, $"{dealNumber}-L1", qty, sellerPrice, sellerPayout, bid.Id);
            _db.DealAllocations.Add(allocation);

            if (buyer is not null && buyer.InfluencerId is not null && buyer.FirstOrderAt is null)
            {
                var influencer = await _db.Influencers.FirstOrDefaultAsync(i => i.Id == buyer.InfluencerId, cancellationToken);
                if (influencer is not null)
                {
                    var commissionAmount = (buyerPrice * qty) * influencer.CommissionRate;
                    var influencerCommission = new InfluencerCommission(buyerPrice * qty, influencer.CommissionRate, commissionAmount, influencer.Id, buyer.Id, deal.Id);
                    _db.InfluencerCommissions.Add(influencerCommission);
                }

                buyer.RecordFirstOrder(now, discountApplied: false);
            }

            buyer?.RecordOrder(buyerPrice * qty);

            bid.MarkAccepted(deal.Id, qty, sellerPrice, now);
            requirement.AdvanceOnAcceptedQuantity(remaining - qty, now);

            await _db.SaveChangesAsync(cancellationToken);

            return deal.Id;
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }
    }

    private async Task<string> NextDealNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"DEAL-{year}-";
        var count = await _db.Deals.CountAsync(d => d.DealNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D5}";
    }
}
