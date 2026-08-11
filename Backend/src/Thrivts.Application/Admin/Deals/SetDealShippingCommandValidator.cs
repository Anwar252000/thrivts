using FluentValidation;

namespace Thrivts.Application.Admin.Deals;

public class SetDealShippingCommandValidator : AbstractValidator<SetDealShippingCommand>
{
    public SetDealShippingCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
    }
}
