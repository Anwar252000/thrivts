using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.ExchangeRates;

public sealed record GetExchangeRatesQuery : IQuery<ErrorOr<List<ExchangeRateDto>>>;

public sealed record ExchangeRateDto(int Id, CurrencyType Currency, decimal RateToUsd, DateTimeOffset EffectiveFrom, string? Notes);

public sealed class GetExchangeRatesQueryHandler : IQueryHandler<GetExchangeRatesQuery, ErrorOr<List<ExchangeRateDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetExchangeRatesQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<ExchangeRateDto>>> Handle(GetExchangeRatesQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list exchange rates.");

        var result = await _db.ExchangeRates.AsNoTracking()
            .OrderByDescending(r => r.EffectiveFrom)
            .Select(r => new ExchangeRateDto(r.Id, r.Currency, r.RateToUsd, r.EffectiveFrom, r.Notes))
            .ToListAsync(cancellationToken);

        return result;
    }
}
