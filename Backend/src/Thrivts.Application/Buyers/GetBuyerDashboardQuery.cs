using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Powers the buyer portal's Dashboard section — mirrors buyer.html's loadDashboard().</summary>
public sealed record GetBuyerDashboardQuery : IQuery<ErrorOr<BuyerDashboardDto>>;

public sealed record BuyerDashboardDto(
    int LiveRequirementsCount, int PendingReviewCount, int OpenDealsCount, int InTransitCount,
    int SettledCount, decimal TotalSpentUsd, List<RecentRequirementDto> RecentRequirements);

public sealed record RecentRequirementDto(
    Guid Id, string RequirementNumber, string ItemName, int QuantityPcs, GradeType Grade,
    string DestinationCountry, string Status, DateTimeOffset CreatedAt);

public sealed class GetBuyerDashboardQueryHandler : IQueryHandler<GetBuyerDashboardQuery, ErrorOr<BuyerDashboardDto>>
{
    private static readonly RequirementStatus[] LiveRequirementStatuses =
        [RequirementStatus.PendingReview, RequirementStatus.Posted, RequirementStatus.Matching, RequirementStatus.ReadyToOrder];

    private static readonly DealStatus[] OpenDealStatuses =
        [DealStatus.Confirmed, DealStatus.AwaitingPayment, DealStatus.Paid, DealStatus.InFulfillment, DealStatus.Dispatched];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBuyerDashboardQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<BuyerDashboardDto>> Handle(GetBuyerDashboardQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var buyerId = _currentUser.UserId.Value;

        var liveRequirementsCount = await _db.Requirements.CountAsync(r => r.BuyerId == buyerId && LiveRequirementStatuses.Contains(r.Status), cancellationToken);
        var pendingReviewCount = await _db.Requirements.CountAsync(r => r.BuyerId == buyerId && r.Status == RequirementStatus.PendingReview, cancellationToken);
        var openDealsCount = await _db.Deals.CountAsync(d => d.BuyerId == buyerId && OpenDealStatuses.Contains(d.Status), cancellationToken);
        var inTransitCount = await _db.Deals.CountAsync(d => d.BuyerId == buyerId && d.Status == DealStatus.Dispatched, cancellationToken);
        var settledCount = await _db.Deals.CountAsync(d => d.BuyerId == buyerId && d.Status == DealStatus.Settled, cancellationToken);
        var totalSpentUsd = await _db.Deals.Where(d => d.BuyerId == buyerId && d.Status == DealStatus.Settled).SumAsync(d => d.TotalInvoiceUsd, cancellationToken);

        var recentRequirements = await _db.Requirements.AsNoTracking()
            .Where(r => r.BuyerId == buyerId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var requirementIds = recentRequirements.Select(r => r.Id).ToArray();
        var dealStatusByRequirementId = await _db.Deals.AsNoTracking()
            .Where(d => requirementIds.Contains(d.RequirementId))
            .ToDictionaryAsync(d => d.RequirementId, d => d.Status, cancellationToken);

        var recentDtos = recentRequirements.Select(r =>
        {
            // A deal's live status is the source of truth once matched — the requirement's own
            // status is frozen at match time (mirrors dash-my-recent's dealStatusByReqNum lookup).
            var status = dealStatusByRequirementId.TryGetValue(r.Id, out var dealStatus) ? dealStatus.ToString() : r.Status.ToString();
            return new RecentRequirementDto(r.Id, r.RequirementNumber, r.ItemName, r.QuantityPcs, r.Grade, r.DestinationCountry, status, r.CreatedAt);
        }).ToList();

        return new BuyerDashboardDto(liveRequirementsCount, pendingReviewCount, openDealsCount, inTransitCount, settledCount, totalSpentUsd, recentDtos);
    }
}
