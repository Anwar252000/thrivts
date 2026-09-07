using FluentValidation;

namespace Thrivts.Application.Sellers;

public class SubmitOrReviseQuoteCommandValidator : AbstractValidator<SubmitOrReviseQuoteCommand>
{
    public SubmitOrReviseQuoteCommandValidator()
    {
        RuleFor(x => x.RequirementId).NotEmpty();
        RuleFor(x => x.AvailableQuantityPcs).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PricePerPcUsd).GreaterThan(0);
        // Matches seller.html's qNotes maxlength="500" — the client already caps this, but the API
        // itself never did.
        RuleFor(x => x.SellerNotes).MaximumLength(500);
    }
}
