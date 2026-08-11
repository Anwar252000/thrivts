using FluentValidation;

namespace Thrivts.Application.Admin.Buyers;

public class UpdateBuyerCommandValidator : AbstractValidator<UpdateBuyerCommand>
{
    public UpdateBuyerCommandValidator()
    {
        RuleFor(x => x.BuyerId).NotEmpty();
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Country).NotEmpty();
    }
}
