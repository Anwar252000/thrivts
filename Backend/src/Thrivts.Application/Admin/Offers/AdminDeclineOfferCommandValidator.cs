using FluentValidation;

namespace Thrivts.Application.Admin.Offers;

public class AdminDeclineOfferCommandValidator : AbstractValidator<AdminDeclineOfferCommand>
{
    public AdminDeclineOfferCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
