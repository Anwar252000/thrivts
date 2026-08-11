using FluentValidation;

namespace Thrivts.Application.Admin.ShippingRates;

public class CreateShippingRateCommandValidator : AbstractValidator<CreateShippingRateCommand>
{
    public CreateShippingRateCommandValidator()
    {
        RuleFor(x => x.DestinationCountry).NotEmpty().MaximumLength(100);
        RuleFor(x => x.RateUsdPerKg).GreaterThan(0).When(x => x.RateUsdPerKg is not null);
        RuleFor(x => x.FlatRateUsd).GreaterThan(0).When(x => x.FlatRateUsd is not null);
        RuleFor(x => x.TransitDays).GreaterThan(0).When(x => x.TransitDays is not null);
        RuleFor(x => x)
            .Must(x => x.RateUsdPerKg is not null || x.FlatRateUsd is not null)
            .WithMessage("Either RateUsdPerKg or FlatRateUsd must be set.");
    }
}
