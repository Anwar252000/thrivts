using FluentValidation;
using Thrivts.Domain.Entities;

namespace Thrivts.Application.Buyers;

public class PostRequirementCommandValidator : AbstractValidator<PostRequirementCommand>
{
    public PostRequirementCommandValidator()
    {
        RuleFor(x => x.ItemName).NotEmpty();
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.QuantityPcs).GreaterThanOrEqualTo(Requirement.MinQuantityPcs);
        RuleFor(x => x.DestinationCountry).NotEmpty();
        RuleFor(x => x.PricePerPc).GreaterThan(0);
    }
}
