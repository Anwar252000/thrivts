using FluentValidation;

namespace Thrivts.Application.Admin.Disputes;

public class RejectDisputeCommandValidator : AbstractValidator<RejectDisputeCommand>
{
    public RejectDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty();
    }
}
