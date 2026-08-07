using FluentValidation;

namespace Thrivts.Application.Admin.Sellers;

public class SetSellerApprovalStatusCommandValidator : AbstractValidator<SetSellerApprovalStatusCommand>
{
    public SetSellerApprovalStatusCommandValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
    }
}
