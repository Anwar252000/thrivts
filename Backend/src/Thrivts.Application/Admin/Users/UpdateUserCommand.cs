using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Users;

/// <summary>
/// Edits the fields shared by every role (Profile itself) — role-specific fields (company name,
/// tier, commission rate, etc.) still go through the existing UpdateBuyer/UpdateSeller/UpdateAgency
/// commands on their own detail pages.
/// </summary>
public sealed record UpdateUserCommand(Guid UserId, string FullName, string? Phone, string? WhatsApp) : ICommand<ErrorOr<Success>>;

public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateUserCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can edit a user.");

        var profile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == command.UserId, cancellationToken);
        if (profile is null)
            return Error.NotFound(description: $"User '{command.UserId}' was not found.");

        profile.UpdateFullName(command.FullName);
        profile.UpdateContactDetails(command.Phone, command.WhatsApp);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
