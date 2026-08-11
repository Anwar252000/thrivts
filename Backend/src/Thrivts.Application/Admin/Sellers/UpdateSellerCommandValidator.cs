using FluentValidation;

namespace Thrivts.Application.Admin.Sellers;

public class UpdateSellerCommandValidator : AbstractValidator<UpdateSellerCommand>
{
    public UpdateSellerCommandValidator()
    {
        RuleFor(x => x.SellerId).NotEmpty();
        RuleFor(x => x.Tier).IsInEnum().When(x => x.Tier is not null);
    }
}
