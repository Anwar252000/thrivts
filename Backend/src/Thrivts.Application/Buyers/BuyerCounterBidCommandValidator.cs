using FluentValidation;

namespace Thrivts.Application.Buyers;

public class BuyerCounterBidCommandValidator : AbstractValidator<BuyerCounterBidCommand>
{
    public BuyerCounterBidCommandValidator()
    {
        RuleFor(x => x.CounterBuyerPriceUsd).GreaterThan(0);
    }
}
