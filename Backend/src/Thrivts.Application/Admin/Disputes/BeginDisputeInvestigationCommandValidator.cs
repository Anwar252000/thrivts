using FluentValidation;

namespace Thrivts.Application.Admin.Disputes;

public class BeginDisputeInvestigationCommandValidator : AbstractValidator<BeginDisputeInvestigationCommand>
{
    public BeginDisputeInvestigationCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty();
    }
}
