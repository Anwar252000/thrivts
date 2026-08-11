using FluentValidation;

namespace Thrivts.Application.Admin.Requirements;

public class DeleteRequirementCommandValidator : AbstractValidator<DeleteRequirementCommand>
{
    public DeleteRequirementCommandValidator()
    {
        RuleFor(x => x.RequirementId).NotEmpty();
    }
}
