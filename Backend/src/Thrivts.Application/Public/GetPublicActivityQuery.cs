using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Public;

/// <summary>Replaces the get_public_activity RPC — an anonymized recent-activity feed for the
/// landing page and the pending/welcome screens. Never exposes buyer/seller identity, only
/// category-level shape (item, grade, quantity, destination) and a coarse activity_type.</summary>
public sealed record GetPublicActivityQuery(int Limit = 20) : IQuery<ErrorOr<List<PublicActivityItemDto>>>;

public sealed record PublicActivityItemDto(
    string ItemName, int QuantityPcs, GradeType Grade, string DestinationCountry, string ActivityType, DateTimeOffset ActivityTime);

public sealed class GetPublicActivityQueryHandler : IQueryHandler<GetPublicActivityQuery, ErrorOr<List<PublicActivityItemDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicActivityQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async ValueTask<ErrorOr<List<PublicActivityItemDto>>> Handle(GetPublicActivityQuery query, CancellationToken cancellationToken)
    {
        var take = query.Limit is > 0 and <= 50 ? query.Limit : 20;

        var postedOrMatching = _db.Requirements.AsNoTracking()
            .Where(r => r.Status == RequirementStatus.Posted || r.Status == RequirementStatus.Matching)
            .Select(r => new PublicActivityItemDto(
                r.ItemName, r.QuantityPcs, r.Grade, r.DestinationCountry,
                r.Status == RequirementStatus.Posted ? "requirement_posted" : "matching",
                r.Status == RequirementStatus.Posted ? (r.PostedAt ?? r.CreatedAt) : (r.MatchedAt ?? r.CreatedAt)));

        var dealActivity =
            from d in _db.Deals.AsNoTracking()
            join r in _db.Requirements.AsNoTracking() on d.RequirementId equals r.Id
            where d.Status == DealStatus.Confirmed || d.Status == DealStatus.Dispatched || d.Status == DealStatus.Settled
            select new PublicActivityItemDto(
                r.ItemName, r.QuantityPcs, r.Grade, r.DestinationCountry,
                d.Status == DealStatus.Confirmed ? "order_confirmed" : d.Status == DealStatus.Dispatched ? "dispatched" : "completed",
                d.Status == DealStatus.Settled ? (d.SettledAt ?? d.CreatedAt) : d.Status == DealStatus.Dispatched ? (d.DispatchedAt ?? d.CreatedAt) : d.CreatedAt);

        var result = await postedOrMatching.Union(dealActivity)
            .OrderByDescending(x => x.ActivityTime)
            .Take(take)
            .ToListAsync(cancellationToken);

        return result;
    }
}
