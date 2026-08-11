using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Settings;

public sealed record UpdatePlatformFeeConfigCommand(decimal FeePerPcUsd, string PkrReference) : ICommand<ErrorOr<Success>>;

public sealed class UpdatePlatformFeeConfigCommandHandler : ICommandHandler<UpdatePlatformFeeConfigCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public UpdatePlatformFeeConfigCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdatePlatformFeeConfigCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can update the platform fee config.");

        var config = await _db.PlatformFeeConfigs.FirstOrDefaultAsync(c => c.Id, cancellationToken);
        if (config is null)
            return Error.NotFound(description: "Platform fee config has not been set up yet.");

        config.UpdateRate(command.FeePerPcUsd, command.PkrReference, _currentUser.UserId.Value, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
