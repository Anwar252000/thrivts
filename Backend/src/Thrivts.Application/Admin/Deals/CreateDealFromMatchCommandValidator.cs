using FluentValidation;

namespace Thrivts.Application.Admin.Deals;

public class CreateDealFromMatchCommandValidator : AbstractValidator<CreateDealFromMatchCommand>
{
    public CreateDealFromMatchCommandValidator()
    {
        RuleFor(x => x.RequirementId).NotEmpty();
        RuleFor(x => x.SellerId).NotEmpty();
        RuleFor(x => x.FinalQuantityPcs).GreaterThan(0);
        RuleFor(x => x.BuyerPricePerPcUsd).GreaterThan(0);
        RuleFor(x => x.SellerCostPerPcUsd).GreaterThan(0);
        RuleFor(x => x.ShippingCostUsd).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BuyerPricePerPcUsd)
            .GreaterThanOrEqualTo(x => x.SellerCostPerPcUsd)
            .WithMessage("Buyer price per piece cannot be less than the seller cost per piece.");
    }
}
