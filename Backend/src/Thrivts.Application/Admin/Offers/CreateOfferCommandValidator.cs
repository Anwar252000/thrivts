using FluentValidation;

namespace Thrivts.Application.Admin.Offers;

public class CreateOfferCommandValidator : AbstractValidator<CreateOfferCommand>
{
    public CreateOfferCommandValidator()
    {
        RuleFor(x => x.RequirementId).NotEmpty();
        RuleFor(x => x.SellerId).NotEmpty();
        RuleFor(x => x.QuantityPcs).GreaterThan(0);
        RuleFor(x => x.OfferPricePerPc).GreaterThan(0);
        RuleFor(x => x.ExpiresInDays).InclusiveBetween(1, 30);
    }
}
