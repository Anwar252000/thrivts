using FluentValidation;

namespace Thrivts.Application.Buyers;

public class RaiseDisputeCommandValidator : AbstractValidator<RaiseDisputeCommand>
{
    public RaiseDisputeCommandValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
    }
}
