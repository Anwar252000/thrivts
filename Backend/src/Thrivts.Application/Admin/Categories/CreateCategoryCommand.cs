using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Categories;

public sealed record CreateCategoryCommand(string Name, string? NameFr, decimal? WeightPerPieceKg, int DisplayOrder) : ICommand<ErrorOr<int>>;

public sealed class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, ErrorOr<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<int>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can create a category.");

        var category = new Category(command.Name, command.NameFr, command.WeightPerPieceKg, command.DisplayOrder);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}
