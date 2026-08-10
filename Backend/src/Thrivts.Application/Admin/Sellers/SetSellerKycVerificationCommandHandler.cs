using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

public sealed class SetSellerKycVerificationCommandHandler
    : ICommandHandler<SetSellerKycVerificationCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SetSellerKycVerificationCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetSellerKycVerificationCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can change a seller's KYC status.");

        var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == command.SellerId, cancellationToken);
        if (seller is null)
            return Error.NotFound(description: $"Seller '{command.SellerId}' was not found.");

        if (command.Verified)
            seller.VerifyKyc(_currentUser.UserId.Value, _clock.UtcNow, command.Notes);
        else
            seller.UnverifyKyc();

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
