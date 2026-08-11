using FluentValidation;

namespace Thrivts.Application.Admin.Influencers;

public class CreateInfluencerCommandValidator : AbstractValidator<CreateInfluencerCommand>
{
    public CreateInfluencerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.CommissionRate).InclusiveBetween(0, 1)
            .WithMessage("CommissionRate is a fraction between 0 and 1 (e.g. 0.05 = 5%).");
    }
}
