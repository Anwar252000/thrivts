using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

public sealed record SetDealShippingCommand(
    Guid DealId, string? ContainerNumber, string? ContainerSize, string? ShippingLine,
    string? VesselName, string? BillOfLading) : ICommand<ErrorOr<Success>>;

public sealed class SetDealShippingCommandHandler : ICommandHandler<SetDealShippingCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SetDealShippingCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SetDealShippingCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can set a deal's shipping details.");

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        deal.SetShippingDetails(command.ContainerNumber, command.ContainerSize, command.ShippingLine,
            command.VesselName, command.BillOfLading);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
