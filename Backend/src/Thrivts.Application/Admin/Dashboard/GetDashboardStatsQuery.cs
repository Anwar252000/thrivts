using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Dashboard;

/// <summary>
/// Replaces admin.html's loadDashboard() — that function never reads a pre-aggregated stats row;
/// it computes every card live from the real tables (profiles/requirements/deals/disputes/
/// commissions) on every dashboard load. The old handler read from PlatformStats, a singleton row
/// nothing in this codebase keeps in sync, so every card silently showed stale/zero data instead
/// of the real counts. This mirrors admin.html's exact filters query-for-query.
/// </summary>
public sealed record GetDashboardStatsQuery : IQuery<ErrorOr<DashboardStatsDto>>;

public sealed record DashboardStatsDto(
    int TotalBuyers, int PendingBuyers,
    int TotalSellers, int PendingSellers,
    int ActiveAgencies, int PendingAgencies,
    int LiveRequirements, int RequirementsAwaitingReview,
    int OpenDisputes,
    decimal TotalVolumeUsd,
    decimal PendingCommissionsUsd, int CommissionsReadyToRelease);

public sealed class GetDashboardStatsQueryHandler : IQueryHandler<GetDashboardStatsQuery, ErrorOr<DashboardStatsDto>>
{
    private static readonly RequirementStatus[] LiveRequirementStatuses = [RequirementStatus.Posted, RequirementStatus.Matching, RequirementStatus.ReadyToOrder];
    private static readonly DisputeStatus[] OpenDisputeStatuses = [DisputeStatus.Open, DisputeStatus.Investigating];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDashboardStatsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<DashboardStatsDto>> Handle(GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view platform stats.");

        var totalBuyers = await _db.Profiles.CountAsync(p => p.Role == UserRole.Buyer, cancellationToken);
        var pendingBuyers = await _db.Profiles.CountAsync(p => p.Role == UserRole.Buyer && p.ApprovalStatus == ApprovalStatus.Pending, cancellationToken);
        var totalSellers = await _db.Profiles.CountAsync(p => p.Role == UserRole.Seller, cancellationToken);
        var pendingSellers = await _db.Profiles.CountAsync(p => p.Role == UserRole.Seller && p.ApprovalStatus == ApprovalStatus.Pending, cancellationToken);
        var activeAgencies = await _db.Profiles.CountAsync(p => p.Role == UserRole.Agency && p.ApprovalStatus == ApprovalStatus.Approved, cancellationToken);
        var pendingAgencies = await _db.Profiles.CountAsync(p => p.Role == UserRole.Agency && p.ApprovalStatus == ApprovalStatus.Pending, cancellationToken);

        var liveRequirements = await _db.Requirements.CountAsync(r => LiveRequirementStatuses.Contains(r.Status), cancellationToken);
        var requirementsAwaitingReview = await _db.Requirements.CountAsync(r => r.Status == RequirementStatus.PendingReview, cancellationToken);

        var openDisputes = await _db.Disputes.CountAsync(d => OpenDisputeStatuses.Contains(d.Status), cancellationToken);

        var totalVolumeUsd = await _db.Deals
            .Where(d => d.Status == DealStatus.Settled)
            .SumAsync(d => (decimal?)d.TotalInvoiceUsd, cancellationToken) ?? 0m;

        var pendingCommissionsUsd = await _db.Commissions
            .Where(c => c.Status == CommissionStatus.Pending)
            .SumAsync(c => (decimal?)c.CommissionAmountUsd, cancellationToken) ?? 0m;
        var commissionsReadyToRelease = await _db.Commissions.CountAsync(c => c.Status == CommissionStatus.ReadyToRelease, cancellationToken);

        return new DashboardStatsDto(
            totalBuyers, pendingBuyers,
            totalSellers, pendingSellers,
            activeAgencies, pendingAgencies,
            liveRequirements, requirementsAwaitingReview,
            openDisputes,
            totalVolumeUsd,
            pendingCommissionsUsd, commissionsReadyToRelease);
    }
}
