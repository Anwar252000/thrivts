using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.ShippingRates;

public sealed record GetShippingRatesQuery : IQuery<ErrorOr<List<ShippingRateDto>>>;

public sealed record ShippingRateDto(
    int Id, string DestinationCountry, decimal? RateUsdPerKg, decimal? FlatRateUsd, int? TransitDays, bool IsActive);

public sealed class GetShippingRatesQueryHandler : IQueryHandler<GetShippingRatesQuery, ErrorOr<List<ShippingRateDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetShippingRatesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<ShippingRateDto>>> Handle(GetShippingRatesQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list shipping rates.");

        var result = await _db.ShippingRates.AsNoTracking()
            .OrderBy(r => r.DestinationCountry)
            .Select(r => new ShippingRateDto(r.Id, r.DestinationCountry, r.RateUsdPerKg, r.FlatRateUsd, r.TransitDays, r.IsActive))
            .ToListAsync(cancellationToken);

        return result;
    }
}
