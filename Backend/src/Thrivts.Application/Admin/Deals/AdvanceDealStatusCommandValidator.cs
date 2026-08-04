using FluentValidation;

namespace Thrivts.Application.Admin.Deals;

public class AdvanceDealStatusCommandValidator : AbstractValidator<AdvanceDealStatusCommand>
{
    public AdvanceDealStatusCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.NewStatus).IsInEnum();
    }
}
