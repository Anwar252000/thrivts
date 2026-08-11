using FluentValidation;

namespace Thrivts.Application.Admin.Requirements;

public class PostRequirementLiveCommandValidator : AbstractValidator<PostRequirementLiveCommand>
{
    public PostRequirementLiveCommandValidator()
    {
        RuleFor(x => x.RequirementId).NotEmpty();
    }
}
