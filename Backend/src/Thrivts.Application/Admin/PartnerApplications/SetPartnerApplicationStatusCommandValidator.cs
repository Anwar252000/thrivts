using FluentValidation;

namespace Thrivts.Application.Admin.PartnerApplications;

public class SetPartnerApplicationStatusCommandValidator : AbstractValidator<SetPartnerApplicationStatusCommand>
{
    public SetPartnerApplicationStatusCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.InfluencerId).NotEmpty().When(x => x.Approve);
    }
}
