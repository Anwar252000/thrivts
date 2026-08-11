using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Approvals;

/// <summary>Powers the Approvals tab (data-tab: all/buyer/seller/agency). Role null means "all".</summary>
public sealed record GetPendingApprovalsQuery(UserRole? Role) : IQuery<ErrorOr<List<PendingApprovalDto>>>;

public sealed record PendingApprovalDto(Guid Id, UserRole Role, string Email, string FullName, DateTimeOffset CreatedAt);

public sealed class GetPendingApprovalsQueryHandler : IQueryHandler<GetPendingApprovalsQuery, ErrorOr<List<PendingApprovalDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPendingApprovalsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<PendingApprovalDto>>> Handle(GetPendingApprovalsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view pending approvals.");

        var profiles = _db.Profiles.AsNoTracking().Where(p => p.ApprovalStatus == ApprovalStatus.Pending);
        if (query.Role is not null)
            profiles = profiles.Where(p => p.Role == query.Role);

        var result = await profiles
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PendingApprovalDto(p.Id, p.Role, p.Email, p.FullName, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
