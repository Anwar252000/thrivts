using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>Replaces editSellerTargetPrice() — sets the max-pay-to-seller ceiling admin uses when
/// sending direct offers. Separate from UpdateRequirementCommand's buyer-facing listing fields
/// since this value is never shown to a seller or buyer.</summary>
public sealed record SetSellerTargetPriceCommand(Guid RequirementId, decimal SellerTargetPriceUsd) : ICommand<ErrorOr<Success>>;

public sealed class SetSellerTargetPriceCommandHandler : ICommandHandler<SetSellerTargetPriceCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetSellerTargetPriceCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetSellerTargetPriceCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can set a requirement's seller target price.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        try
        {
            requirement.SetSellerTargetPrice(command.SellerTargetPriceUsd);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
