using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Auth;

/// <summary>
/// Backs GET /api/v1/auth/me — the only way the frontend learns its own role, since the browser
/// never talks to Supabase (or the profiles table) directly. Read straight from Profiles rather
/// than trusting a client-supplied value, matching every other handler's rule of never trusting
/// the client for identity.
/// </summary>
public sealed record GetCurrentUserQuery : IQuery<ErrorOr<CurrentUserDto>>;

public sealed record CurrentUserDto(Guid Id, string Email, string FullName, UserRole Role, ApprovalStatus ApprovalStatus, bool IsActive, string? RejectionReason);

public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, ErrorOr<CurrentUserDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetCurrentUserQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<CurrentUserDto>> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var profile = await _db.Profiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == _currentUser.UserId.Value, cancellationToken);

        if (profile is null)
            return Error.NotFound(description: "Profile not found.");

        return new CurrentUserDto(profile.Id, profile.Email, profile.FullName, profile.Role, profile.ApprovalStatus, profile.IsActive, profile.RejectionReason);
    }
}
