using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.ShippingRates;

public sealed record UpdateShippingRateCommand(int ShippingRateId, decimal? RateUsdPerKg, decimal? FlatRateUsd, bool IsActive) : ICommand<ErrorOr<Success>>;

public sealed class UpdateShippingRateCommandHandler : ICommandHandler<UpdateShippingRateCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateShippingRateCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(UpdateShippingRateCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can edit a shipping rate.");

        var rate = await _db.ShippingRates.FirstOrDefaultAsync(r => r.Id == command.ShippingRateId, cancellationToken);
        if (rate is null)
            return Error.NotFound(description: $"Shipping rate '{command.ShippingRateId}' was not found.");

        rate.UpdateRate(command.RateUsdPerKg, command.FlatRateUsd);
        if (command.IsActive) rate.Reactivate();
        else rate.Deactivate();

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
