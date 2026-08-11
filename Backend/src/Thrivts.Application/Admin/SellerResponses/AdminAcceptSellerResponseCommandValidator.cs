using FluentValidation;

namespace Thrivts.Application.Admin.SellerResponses;

public class AdminAcceptSellerResponseCommandValidator : AbstractValidator<AdminAcceptSellerResponseCommand>
{
    public AdminAcceptSellerResponseCommandValidator()
    {
        RuleFor(x => x.SellerResponseId).NotEmpty();
    }
}
