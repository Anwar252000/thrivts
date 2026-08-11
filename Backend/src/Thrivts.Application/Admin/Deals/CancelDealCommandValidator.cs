using FluentValidation;

namespace Thrivts.Application.Admin.Deals;

public class CancelDealCommandValidator : AbstractValidator<CancelDealCommand>
{
    public CancelDealCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
