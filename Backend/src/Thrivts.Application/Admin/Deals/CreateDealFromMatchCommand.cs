using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

/// <summary>
/// Replaces create_deal_from_match — admin manually matches a bid/offer to a deal (bypassing the
/// normal buyer-accept flow). Money model matches the live admin.html calculator: spread/pc =
/// buyerPrice - sellerCost, total spread = spread/pc * qty - shipping. Also creates the matching
/// DealAllocation row (the original create_deal_from_match RPC does both in one transaction) and,
/// if the buyer is agency-attributed or influencer-referred, attributes the deal accordingly.
/// </summary>
public sealed record CreateDealFromMatchCommand(
    Guid RequirementId,
    Guid SellerId,
    int FinalQuantityPcs,
    decimal BuyerPricePerPcUsd,
    decimal SellerCostPerPcUsd,
    decimal ShippingCostUsd,
    Guid? SourceResponseId,
    Guid? SourceOfferId,
    DateOnly? EstimatedDispatchDate,
    string? AdminNotes) : ICommand<ErrorOr<Guid>>;

public sealed class CreateDealFromMatchCommandHandler : ICommandHandler<CreateDealFromMatchCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public CreateDealFromMatchCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(CreateDealFromMatchCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can create a deal from a match.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        var buyer = await _db.Buyers.FirstOrDefaultAsync(b => b.Id == requirement.BuyerId, cancellationToken);

        var spreadPerPc = command.BuyerPricePerPcUsd - command.SellerCostPerPcUsd;
        var subtotal = command.BuyerPricePerPcUsd * command.FinalQuantityPcs;
        var totalInvoice = subtotal + command.ShippingCostUsd;
        var totalSpread = spreadPerPc * command.FinalQuantityPcs - command.ShippingCostUsd;
        var totalSellerPayout = command.SellerCostPerPcUsd * command.FinalQuantityPcs;
        var now = _clock.UtcNow;

        var dealNumber = await NextDealNumberAsync(cancellationToken);

        var deal = new Deal(
            dealNumber: dealNumber,
            requirementId: command.RequirementId,
            buyerId: requirement.BuyerId,
            sellerId: command.SellerId,
            totalQuantityPcs: command.FinalQuantityPcs,
            buyerPricePerPcUsd: command.BuyerPricePerPcUsd,
            avgSellerPricePerPcUsd: command.SellerCostPerPcUsd,
            spreadPerPcUsd: spreadPerPc,
            subtotalUsd: subtotal,
            totalInvoiceUsd: totalInvoice,
            totalSpreadUsd: totalSpread,
            totalSellerPayoutUsd: totalSellerPayout,
            agencyId: buyer?.AttributedToAgency,
            sourceResponseId: command.SourceResponseId,
            sourceOfferId: command.SourceOfferId,
            createdBy: _currentUser.UserId.Value);

        deal.SetEstimatedDates(command.EstimatedDispatchDate, null, null);
        deal.AdvanceTo(DealStatus.Confirmed, now);

        _db.Deals.Add(deal);

        var allocation = new DealAllocation(deal.Id, command.SellerId, $"{dealNumber}-L1",
            command.FinalQuantityPcs, command.SellerCostPerPcUsd, totalSellerPayout, command.SourceResponseId);
        _db.DealAllocations.Add(allocation);

        if (buyer is not null && buyer.InfluencerId is not null && buyer.FirstOrderAt is null)
        {
            var influencer = await _db.Influencers.FirstOrDefaultAsync(i => i.Id == buyer.InfluencerId, cancellationToken);
            if (influencer is not null)
            {
                var commissionAmount = totalInvoice * influencer.CommissionRate;
                var influencerCommission = new InfluencerCommission(totalInvoice, influencer.CommissionRate, commissionAmount, influencer.Id, buyer.Id, deal.Id);
                _db.InfluencerCommissions.Add(influencerCommission);
            }

            buyer.RecordFirstOrder(now, discountApplied: false);
        }

        buyer?.RecordOrder(totalInvoice);
        requirement.AdvanceOnAcceptedQuantity(0, now);

        await _db.SaveChangesAsync(cancellationToken);

        return deal.Id;
    }

    private async Task<string> NextDealNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"DEAL-{year}-";
        var count = await _db.Deals.CountAsync(d => d.DealNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D5}";
    }
}
