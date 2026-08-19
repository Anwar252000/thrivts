using FluentValidation;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Users;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).WithMessage("Password must be at least 8 characters.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Role).IsInEnum();

        RuleFor(x => x.CompanyName).NotEmpty().When(x => x.Role == UserRole.Buyer)
            .WithMessage("Company name is required for a buyer.");
        RuleFor(x => x.Country).NotEmpty().When(x => x.Role is UserRole.Buyer or UserRole.Agency)
            .WithMessage("Country is required.");

        RuleFor(x => x.LocationCity).NotEmpty().When(x => x.Role == UserRole.Seller)
            .WithMessage("City is required for a seller.");

        RuleFor(x => x.AgencyName).NotEmpty().When(x => x.Role == UserRole.Agency)
            .WithMessage("Agency name is required for an agency.");
        RuleFor(x => x.CommissionRate).InclusiveBetween(0, 100).When(x => x.CommissionRate is not null);
    }
}
