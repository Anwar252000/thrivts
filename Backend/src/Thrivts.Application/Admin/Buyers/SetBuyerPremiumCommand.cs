using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

/// <summary>Replaces admin.html's setPremium() — marks/unmarks a buyer with the "Premium ★" flag.</summary>
public sealed record SetBuyerPremiumCommand(Guid BuyerId, bool IsPremium) : ICommand<ErrorOr<Success>>;

public sealed class SetBuyerPremiumCommandHandler : ICommandHandler<SetBuyerPremiumCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetBuyerPremiumCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetBuyerPremiumCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can change a buyer's premium status.");

        var buyer = await _db.Buyers.FirstOrDefaultAsync(b => b.Id == command.BuyerId, cancellationToken);
        if (buyer is null)
            return Error.NotFound(description: $"Buyer '{command.BuyerId}' was not found.");

        buyer.SetPremium(command.IsPremium);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
