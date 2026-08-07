using FluentValidation;

namespace Thrivts.Application.Admin.Buyers;

public class SetBuyerApprovalStatusCommandValidator : AbstractValidator<SetBuyerApprovalStatusCommand>
{
    public SetBuyerApprovalStatusCommandValidator()
    {
        RuleFor(x => x.BuyerId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
    }
}
