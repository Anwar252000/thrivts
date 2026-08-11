using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Commissions;

/// <summary>Powers the Commissions tab (data-tab: pending/ready_to_release/released).</summary>
public sealed record GetCommissionsQuery(CommissionStatus? Status) : IQuery<ErrorOr<List<CommissionListItemDto>>>;

public sealed record CommissionListItemDto(
    Guid Id, Guid DealId, Guid AgencyId, decimal CommissionRate, decimal CommissionAmountUsd,
    CommissionStatus Status, DateTimeOffset ReleaseDueAt, DateTimeOffset? ReleasedAt, DateTimeOffset CreatedAt);

public sealed class GetCommissionsQueryHandler : IQueryHandler<GetCommissionsQuery, ErrorOr<List<CommissionListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCommissionsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<CommissionListItemDto>>> Handle(GetCommissionsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list commissions.");

        var commissions = _db.Commissions.AsNoTracking();
        if (query.Status is not null)
            commissions = commissions.Where(c => c.Status == query.Status);

        var result = await commissions
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CommissionListItemDto(
                c.Id, c.DealId, c.AgencyId, c.CommissionRate, c.CommissionAmountUsd,
                c.Status, c.ReleaseDueAt, c.ReleasedAt, c.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
