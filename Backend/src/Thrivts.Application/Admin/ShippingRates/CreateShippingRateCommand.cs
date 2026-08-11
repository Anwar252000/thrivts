using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.ShippingRates;

public sealed record CreateShippingRateCommand(
    string DestinationCountry, decimal? RateUsdPerKg, decimal? FlatRateUsd, int? TransitDays) : ICommand<ErrorOr<int>>;

public sealed class CreateShippingRateCommandHandler : ICommandHandler<CreateShippingRateCommand, ErrorOr<int>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateShippingRateCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<int>> Handle(CreateShippingRateCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can create a shipping rate.");

        var rate = new ShippingRate(command.DestinationCountry, command.RateUsdPerKg, command.FlatRateUsd, command.TransitDays);
        _db.ShippingRates.Add(rate);
        await _db.SaveChangesAsync(cancellationToken);

        return rate.Id;
    }
}
