using FluentValidation;

namespace Thrivts.Application.Auth;

public class RegisterBuyerCommandValidator : AbstractValidator<RegisterBuyerCommand>
{
    public RegisterBuyerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.CompanyName).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}
