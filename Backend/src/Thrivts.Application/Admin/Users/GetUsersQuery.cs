using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Users;

/// <summary>
/// Powers a single unified "all users" screen across every role — the role-specific admin pages
/// (Buyers/Sellers/Agencies) already exist for role-specific detail and stay as-is; this is a
/// flat, role-agnostic view for quickly finding/managing any account regardless of role.
/// </summary>
public sealed record GetUsersQuery(UserRole? Role, string? Search, int Page, int PageSize) : IQuery<ErrorOr<PagedResult<UserListItemDto>>>;

public sealed record UserListItemDto(
    Guid Id, string Email, string FullName, UserRole Role, string? Phone, string? WhatsApp,
    ApprovalStatus ApprovalStatus, bool IsActive, DateTimeOffset CreatedAt);

public sealed class GetUsersQueryHandler : IQueryHandler<GetUsersQuery, ErrorOr<PagedResult<UserListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUsersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PagedResult<UserListItemDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list users.");

        var (page, pageSize) = PagedResult<UserListItemDto>.Normalize(query.Page, query.PageSize);

        var profiles = _db.Profiles.AsNoTracking();

        if (query.Role is not null)
            profiles = profiles.Where(p => p.Role == query.Role);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            profiles = profiles.Where(p => p.FullName.ToLower().Contains(search) || p.Email.ToLower().Contains(search));
        }

        var totalCount = await profiles.CountAsync(cancellationToken);
        var items = await profiles
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new UserListItemDto(p.Id, p.Email, p.FullName, p.Role, p.Phone, p.WhatsApp, p.ApprovalStatus, p.IsActive, p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserListItemDto>(items, totalCount, page, pageSize);
    }
}
