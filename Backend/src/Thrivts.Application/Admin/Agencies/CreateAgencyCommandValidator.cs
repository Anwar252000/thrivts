using FluentValidation;

namespace Thrivts.Application.Admin.Agencies;

public class CreateAgencyCommandValidator : AbstractValidator<CreateAgencyCommand>
{
    public CreateAgencyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.AgencyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OwnerFullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Country).NotEmpty();
        RuleFor(x => x.CommissionRate).InclusiveBetween(0, 100);
        RuleFor(x => x.TeamSize).GreaterThan(0).When(x => x.TeamSize is not null);
    }
}
