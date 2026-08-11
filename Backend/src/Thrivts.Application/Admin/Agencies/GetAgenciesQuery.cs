using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Agencies;

public sealed record GetAgenciesQuery : IQuery<ErrorOr<List<AgencyListItemDto>>>;

public sealed record AgencyListItemDto(
    Guid Id, string AgencyName, string AgencyCode, string OwnerFullName, string Country,
    decimal CommissionRate, bool IsActive, int TotalBuyersReferred, int TotalDealsClosed,
    decimal TotalCommissionEarnedUsd, decimal TotalCommissionPendingUsd, DateTimeOffset CreatedAt);

public sealed class GetAgenciesQueryHandler : IQueryHandler<GetAgenciesQuery, ErrorOr<List<AgencyListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAgenciesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<AgencyListItemDto>>> Handle(GetAgenciesQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list agencies.");

        var result = await _db.Agencies.AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AgencyListItemDto(
                a.Id, a.AgencyName, a.AgencyCode, a.OwnerFullName, a.Country,
                a.CommissionRate, a.IsActive, a.TotalBuyersReferred, a.TotalDealsClosed,
                a.TotalCommissionEarnedUsd, a.TotalCommissionPendingUsd, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
