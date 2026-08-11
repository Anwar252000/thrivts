using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Categories;

/// <summary>Closes README_HANDOVER.md's "Category edit/delete UI (currently add-only)" fast-follow.</summary>
public sealed record UpdateCategoryCommand(int CategoryId, string Name, string? NameFr, bool IsActive) : ICommand<ErrorOr<Success>>;

public sealed class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateCategoryCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can edit a category.");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);
        if (category is null)
            return Error.NotFound(description: $"Category '{command.CategoryId}' was not found.");

        category.Rename(command.Name, command.NameFr);
        if (command.IsActive) category.Reactivate();
        else category.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
