using FluentValidation;

namespace Thrivts.Application.Admin.Requirements;

public class UpdateRequirementCommandValidator : AbstractValidator<UpdateRequirementCommand>
{
    public UpdateRequirementCommandValidator()
    {
        RuleFor(x => x.RequirementId).NotEmpty();
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.QuantityPcs).GreaterThan(0);
        RuleFor(x => x.Grade).IsInEnum();
        RuleFor(x => x.DestinationCountry).NotEmpty();
        RuleFor(x => x.BuyerTargetPriceUsd).GreaterThan(0);
    }
}
