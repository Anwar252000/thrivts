using FluentValidation;

namespace Thrivts.Application.Admin.Deals;

public class SetDealTrackingCommandValidator : AbstractValidator<SetDealTrackingCommand>
{
    public SetDealTrackingCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
    }
}
