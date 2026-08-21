using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces the buyer_visible_bids view — fee-inclusive, anonymized bids for one of the
/// caller's own requirements. Never exposes seller identity, CurrentPriceUsd/ProposedPriceUsd, or
/// FeePerPcAppliedUsd individually (see SellerResponse.BuyerPricePerPcUsd's doc comment — the fee
/// split is the moat rule this DTO must never leak).</summary>
public sealed record GetBuyerVisibleBidsQuery(Guid RequirementId) : IQuery<ErrorOr<List<BuyerVisibleBidDto>>>;

public sealed record BuyerVisibleBidDto(
    Guid BidId, int AvailableQuantityPcs, decimal BuyerPricePerPcUsd, decimal BuyerTotalUsd,
    BidStatus Status, NegotiationState NegotiationState, int RoundCount,
    string SellerAlias, SellerTier SellerTier, bool SellerVerified, DateTimeOffset? BidTime);

public sealed class GetBuyerVisibleBidsQueryHandler : IQueryHandler<GetBuyerVisibleBidsQuery, ErrorOr<List<BuyerVisibleBidDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBuyerVisibleBidsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<BuyerVisibleBidDto>>> Handle(GetBuyerVisibleBidsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var requirement = await _db.Requirements.AsNoTracking().FirstOrDefaultAsync(r => r.Id == query.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{query.RequirementId}' was not found.");
        if (requirement.BuyerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only view bids on your own requirements.");

        var result = await (
            from sr in _db.SellerResponses.AsNoTracking()
            join s in _db.Sellers.AsNoTracking() on sr.SellerId equals s.Id
            where sr.RequirementId == query.RequirementId
            orderby (sr.CurrentPriceUsd ?? 0) + (sr.FeePerPcAppliedUsd ?? 0)
            select new BuyerVisibleBidDto(
                sr.Id, sr.AvailableQuantityPcs, (sr.CurrentPriceUsd ?? 0) + (sr.FeePerPcAppliedUsd ?? 0),
                ((sr.CurrentPriceUsd ?? 0) + (sr.FeePerPcAppliedUsd ?? 0)) * sr.AvailableQuantityPcs,
                sr.Status, sr.NegotiationState, sr.RoundCount, s.PublicAlias, s.Tier, s.KycVerified, sr.RespondedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
