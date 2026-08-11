using FluentValidation;

namespace Thrivts.Application.Admin.Agencies;

public class SetAgencyApprovalStatusCommandValidator : AbstractValidator<SetAgencyApprovalStatusCommand>
{
    public SetAgencyApprovalStatusCommandValidator()
    {
        RuleFor(x => x.AgencyId).NotEmpty();
        RuleFor(x => x.Action).IsInEnum();
    }
}
