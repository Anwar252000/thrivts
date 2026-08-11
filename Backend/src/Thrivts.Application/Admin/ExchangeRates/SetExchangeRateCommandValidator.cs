using FluentValidation;

namespace Thrivts.Application.Admin.ExchangeRates;

public class SetExchangeRateCommandValidator : AbstractValidator<SetExchangeRateCommand>
{
    public SetExchangeRateCommandValidator()
    {
        RuleFor(x => x.Currency).IsInEnum();
        RuleFor(x => x.RateToUsd).GreaterThan(0);
    }
}
