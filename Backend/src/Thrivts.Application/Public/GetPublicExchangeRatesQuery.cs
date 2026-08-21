using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Public;

/// <summary>Latest rate per currency, display-only — a thinner, non-admin-gated sibling of
/// Admin.ExchangeRates.GetExchangeRatesQuery (which stays admin-only since it also exposes
/// id/notes/effectiveFrom). Used for the buyer's live price-preview only; PostRequirementCommand
/// always recomputes the trusted USD conversion server-side, never trusting this value back.</summary>
public sealed record GetPublicExchangeRatesQuery : IQuery<ErrorOr<List<PublicExchangeRateDto>>>;

public sealed record PublicExchangeRateDto(CurrencyType Currency, decimal RateToUsd);

public sealed class GetPublicExchangeRatesQueryHandler : IQueryHandler<GetPublicExchangeRatesQuery, ErrorOr<List<PublicExchangeRateDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetPublicExchangeRatesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async ValueTask<ErrorOr<List<PublicExchangeRateDto>>> Handle(GetPublicExchangeRatesQuery query, CancellationToken cancellationToken)
    {
        var rates = await _db.ExchangeRates.AsNoTracking()
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync(cancellationToken);

        var latestByCurrency = rates
            .GroupBy(r => r.Currency)
            .Select(g => new PublicExchangeRateDto(g.Key, g.First().RateToUsd))
            .ToList();

        return latestByCurrency;
    }
}
