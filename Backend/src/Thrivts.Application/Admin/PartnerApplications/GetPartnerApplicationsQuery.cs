using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.PartnerApplications;

/// <summary>Replaces admin_list_partner_applications.</summary>
public sealed record GetPartnerApplicationsQuery(string? Status) : IQuery<ErrorOr<List<PartnerApplicationDto>>>;

public sealed record PartnerApplicationDto(
    Guid Id, string FullName, string Email, string? Instagram, string? Tiktok, string? Platform,
    string? ProfileLink, string Status, DateTimeOffset CreatedAt);

public sealed class GetPartnerApplicationsQueryHandler : IQueryHandler<GetPartnerApplicationsQuery, ErrorOr<List<PartnerApplicationDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPartnerApplicationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<PartnerApplicationDto>>> Handle(GetPartnerApplicationsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list partner applications.");

        var applications = _db.PartnerApplications.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Status))
            applications = applications.Where(a => a.Status == query.Status);

        var result = await applications
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PartnerApplicationDto(a.Id, a.FullName, a.Email, a.Instagram, a.Tiktok, a.Platform, a.ProfileLink, a.Status, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
