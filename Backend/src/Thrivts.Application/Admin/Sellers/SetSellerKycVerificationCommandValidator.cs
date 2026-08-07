using FluentValidation;

namespace Thrivts.Application.Admin.Sellers;

public class SetSellerKycVerificationCommandValidator : AbstractValidator<SetSellerKycVerificationCommand>
{
    public SetSellerKycVerificationCommandValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty();
    }
}
