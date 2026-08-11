using FluentValidation;

namespace Thrivts.Application.Admin.Disputes;

public class ResolveDisputeCommandValidator : AbstractValidator<ResolveDisputeCommand>
{
    public ResolveDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId).NotEmpty();
        RuleFor(x => x.Resolution).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.RefundAmountUsd).GreaterThanOrEqualTo(0);
    }
}
