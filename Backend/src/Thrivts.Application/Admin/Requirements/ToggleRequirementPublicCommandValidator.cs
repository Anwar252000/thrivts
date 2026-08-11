using FluentValidation;

namespace Thrivts.Application.Admin.Requirements;

public class ToggleRequirementPublicCommandValidator : AbstractValidator<ToggleRequirementPublicCommand>
{
    public ToggleRequirementPublicCommandValidator()
    {
        RuleFor(x => x.RequirementId).NotEmpty();
    }
}
