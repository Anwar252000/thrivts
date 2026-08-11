using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Admin.ExchangeRates;

/// <summary>Replaces the direct exchange_rates.insert — each new rate is a new row (history is kept).</summary>
public sealed record SetExchangeRateCommand(CurrencyType Currency, decimal RateToUsd, string? Notes) : ICommand<ErrorOr<int>>;

public sealed class SetExchangeRateCommandHandler : ICommandHandler<SetExchangeRateCommand, ErrorOr<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetExchangeRateCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<int>> Handle(SetExchangeRateCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can set exchange rates.");

        try
        {
            var rate = new ExchangeRate(command.Currency, command.RateToUsd, _currentUser.UserId, command.Notes);
            _db.ExchangeRates.Add(rate);
            await _db.SaveChangesAsync(cancellationToken);

            return rate.Id;
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }
    }
}
