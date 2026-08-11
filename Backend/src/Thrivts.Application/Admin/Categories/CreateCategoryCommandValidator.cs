using FluentValidation;

namespace Thrivts.Application.Admin.Categories;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WeightPerPieceKg).GreaterThan(0).When(x => x.WeightPerPieceKg is not null);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
