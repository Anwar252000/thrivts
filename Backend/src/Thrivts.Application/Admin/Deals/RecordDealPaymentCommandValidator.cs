using FluentValidation;

namespace Thrivts.Application.Admin.Deals;

public class RecordDealPaymentCommandValidator : AbstractValidator<RecordDealPaymentCommand>
{
    public RecordDealPaymentCommandValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.PaymentMethod).NotEmpty().MaximumLength(100);
    }
}
