using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Sellers;

/// <summary>Replaces the seller_own_bids view read — the seller's own bid board, with the buyer's
/// counter (if any) and the exact net-payout math the view computes.</summary>
public sealed record GetMyQuotesQuery : IQuery<ErrorOr<List<MyQuoteDto>>>;

public sealed record MyQuoteDto(
    Guid BidId, Guid RequirementId, string RequirementNumber, string ItemName, int RequirementQtyPcs, GradeType Grade,
    string DestinationCountry, int AvailableQuantityPcs, decimal YourPricePerPcUsd, decimal PlatformFeePerPcUsd,
    decimal YouReceivePerPcUsd, decimal YouReceiveTotalUsd, decimal? BuyerCounterPriceUsd, decimal? CounterYouReceivePerPc,
    DateTimeOffset? BuyerCounterAt, string? BuyerCounterNote, NegotiationState NegotiationState, NegotiationActor? LastActor,
    DateTimeOffset? LastActionAt, string StatusLabel, BidStatus Status, DateTimeOffset? RespondedAt, string? SellerNotes);

public sealed class GetMyQuotesQueryHandler : IQueryHandler<GetMyQuotesQuery, ErrorOr<List<MyQuoteDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyQuotesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<MyQuoteDto>>> Handle(GetMyQuotesQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var bids = await (
            from sr in _db.SellerResponses.AsNoTracking()
            join r in _db.Requirements.AsNoTracking() on sr.RequirementId equals r.Id
            where sr.SellerId == _currentUser.UserId
            orderby sr.RespondedAt descending
            select new { Bid = sr, Requirement = r })
            .ToListAsync(cancellationToken);

        var result = bids.Select(x =>
        {
            var sr = x.Bid;
            var yourPrice = sr.CurrentPriceUsd ?? sr.ProposedPriceUsd ?? 0;
            var fee = sr.FeePerPcAppliedUsd ?? 0.70m;
            var youReceivePerPc = Math.Max(Math.Round(yourPrice - fee, 2), 0);
            var youReceiveTotal = Math.Round(youReceivePerPc * sr.AvailableQuantityPcs, 2);
            var counterYouReceive = sr.BuyerCounterPriceUsd is null ? (decimal?)null : Math.Round(sr.BuyerCounterPriceUsd.Value - fee, 2);

            var statusLabel = sr.NegotiationState switch
            {
                NegotiationState.CounteredByBuyer => "Buyer countered — your move",
                NegotiationState.CounteredBySeller => "You countered — awaiting buyer",
                NegotiationState.Declined => "You declined the counter",
                NegotiationState.Accepted => "Accepted — deal created",
                _ => "Bid submitted — awaiting buyer",
            };

            return new MyQuoteDto(
                sr.Id, sr.RequirementId, x.Requirement.RequirementNumber, x.Requirement.ItemName, x.Requirement.QuantityPcs,
                x.Requirement.Grade, x.Requirement.DestinationCountry, sr.AvailableQuantityPcs, yourPrice, fee,
                youReceivePerPc, youReceiveTotal, sr.BuyerCounterPriceUsd, counterYouReceive, sr.BuyerCounterAt,
                sr.BuyerCounterNote, sr.NegotiationState, sr.LastActor, sr.LastActionAt, statusLabel, sr.Status, sr.RespondedAt, sr.SellerNotes);
        }).ToList();

        return result;
    }
}
