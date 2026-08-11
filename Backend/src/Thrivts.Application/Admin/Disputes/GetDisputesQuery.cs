using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Disputes;

/// <summary>Powers the Disputes tab (data-tab: open/under_review/resolved).</summary>
public sealed record GetDisputesQuery(DisputeStatus? Status) : IQuery<ErrorOr<List<DisputeListItemDto>>>;

public sealed record DisputeListItemDto(
    Guid Id, string DisputeNumber, Guid DealId, Guid RaisedBy, string? Category, string Description,
    DisputeStatus Status, decimal RefundAmountUsd, DateTimeOffset CreatedAt);

public sealed class GetDisputesQueryHandler : IQueryHandler<GetDisputesQuery, ErrorOr<List<DisputeListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDisputesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<DisputeListItemDto>>> Handle(GetDisputesQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list disputes.");

        var disputes = _db.Disputes.AsNoTracking();
        if (query.Status is not null)
            disputes = disputes.Where(d => d.Status == query.Status);

        var result = await disputes
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DisputeListItemDto(d.Id, d.DisputeNumber, d.DealId, d.RaisedBy, d.Category, d.Description, d.Status, d.RefundAmountUsd, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
