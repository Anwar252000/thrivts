using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Agencies;

public sealed record GetAgencyByIdQuery(Guid AgencyId) : IQuery<ErrorOr<AgencyDetailDto>>;

public sealed record AgencyDetailDto(
    Guid Id, string Email, string AgencyName, string AgencyCode, string OwnerFullName, string Country, string? City,
    int? TeamSize, decimal CommissionRate, int TotalBuyersReferred, int TotalDealsClosed,
    decimal TotalCommissionEarnedUsd, decimal TotalCommissionPaidUsd, decimal TotalCommissionPendingUsd,
    bool IsActive, string? Notes, DateTimeOffset CreatedAt);

public sealed class GetAgencyByIdQueryHandler : IQueryHandler<GetAgencyByIdQuery, ErrorOr<AgencyDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAgencyByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<AgencyDetailDto>> Handle(GetAgencyByIdQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view agency details.");

        var result = await (
                from agency in _db.Agencies.AsNoTracking()
                join profile in _db.Profiles.AsNoTracking() on agency.Id equals profile.Id
                where agency.Id == query.AgencyId
                select new AgencyDetailDto(
                    agency.Id, profile.Email, agency.AgencyName, agency.AgencyCode, agency.OwnerFullName, agency.Country, agency.City,
                    agency.TeamSize, agency.CommissionRate, agency.TotalBuyersReferred, agency.TotalDealsClosed,
                    agency.TotalCommissionEarnedUsd, agency.TotalCommissionPaidUsd, agency.TotalCommissionPendingUsd,
                    agency.IsActive, agency.Notes, agency.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? Error.NotFound(description: $"Agency '{query.AgencyId}' was not found.")
            : result;
    }
}
