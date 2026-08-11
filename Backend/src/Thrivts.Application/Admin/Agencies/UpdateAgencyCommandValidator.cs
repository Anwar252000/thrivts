using FluentValidation;

namespace Thrivts.Application.Admin.Agencies;

public class UpdateAgencyCommandValidator : AbstractValidator<UpdateAgencyCommand>
{
    public UpdateAgencyCommandValidator()
    {
        RuleFor(x => x.AgencyId).NotEmpty();
        RuleFor(x => x.CommissionRate).InclusiveBetween(0, 100).When(x => x.CommissionRate is not null);
    }
}
