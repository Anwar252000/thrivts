using FluentValidation;

namespace Thrivts.Application.Admin.Offers;

public class PostOfferRoundCommandValidator : AbstractValidator<PostOfferRoundCommand>
{
    public PostOfferRoundCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Kind).NotEmpty().Must(k => k is "counter" or "message")
            .WithMessage("Kind must be 'counter' or 'message'.");
        RuleFor(x => x.PricePerPcUsd).GreaterThan(0).When(x => x.PricePerPcUsd is not null);
    }
}
