using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Audit;

public sealed record GetAuditLogQuery(int Take) : IQuery<ErrorOr<List<AuditLogEntryDto>>>;

public sealed record AuditLogEntryDto(
    Guid Id, Guid? ActorId, UserRole? ActorRole, string Action, string EntityType, Guid? EntityId,
    string? DetailsJson, DateTimeOffset CreatedAt);

public sealed class GetAuditLogQueryHandler : IQueryHandler<GetAuditLogQuery, ErrorOr<List<AuditLogEntryDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAuditLogQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<AuditLogEntryDto>>> Handle(GetAuditLogQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view the audit log.");

        var take = query.Take is > 0 and <= 500 ? query.Take : 100;

        var result = await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(a => new AuditLogEntryDto(a.Id, a.ActorId, a.ActorRole, a.Action, a.EntityType, a.EntityId, a.DetailsJson, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
