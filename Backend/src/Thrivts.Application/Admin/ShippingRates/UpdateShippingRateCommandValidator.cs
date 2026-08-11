using FluentValidation;

namespace Thrivts.Application.Admin.ShippingRates;

public class UpdateShippingRateCommandValidator : AbstractValidator<UpdateShippingRateCommand>
{
    public UpdateShippingRateCommandValidator()
    {
        RuleFor(x => x.ShippingRateId).GreaterThan(0);
        RuleFor(x => x.RateUsdPerKg).GreaterThan(0).When(x => x.RateUsdPerKg is not null);
        RuleFor(x => x.FlatRateUsd).GreaterThan(0).When(x => x.FlatRateUsd is not null);
    }
}
