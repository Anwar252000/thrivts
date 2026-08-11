using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Dashboard;

public sealed record GetDashboardStatsQuery : IQuery<ErrorOr<DashboardStatsDto>>;

public sealed record DashboardStatsDto(
    int TotalRequirementsPosted, int TotalDealsClosed, long TotalPcsMoved, decimal TotalVolumeUsd,
    int ActiveBuyers, int ActiveSellers, int ActiveAgencies, int CountriesServed,
    int TodayRequirements, int TodayMatches, int TodayDispatched,
    int ThisMonthDeals, decimal ThisMonthVolumeUsd, DateTimeOffset LastUpdatedAt);

public sealed class GetDashboardStatsQueryHandler : IQueryHandler<GetDashboardStatsQuery, ErrorOr<DashboardStatsDto>>
{
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

        var stats = await _db.PlatformStats.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, cancellationToken);
        if (stats is null)
            return Error.NotFound(description: "Platform stats have not been computed yet.");

        return new DashboardStatsDto(
            stats.TotalRequirementsPosted, stats.TotalDealsClosed, stats.TotalPcsMoved, stats.TotalVolumeUsd,
            stats.ActiveBuyers, stats.ActiveSellers, stats.ActiveAgencies, stats.CountriesServed,
            stats.TodayRequirements, stats.TodayMatches, stats.TodayDispatched,
            stats.ThisMonthDeals, stats.ThisMonthVolumeUsd, stats.LastUpdatedAt);
    }
}
