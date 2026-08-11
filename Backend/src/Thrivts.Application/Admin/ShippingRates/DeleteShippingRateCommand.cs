using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.ShippingRates;

public sealed record DeleteShippingRateCommand(int ShippingRateId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteShippingRateCommandHandler : ICommandHandler<DeleteShippingRateCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteShippingRateCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteShippingRateCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can delete a shipping rate.");

        var rate = await _db.ShippingRates.FirstOrDefaultAsync(r => r.Id == command.ShippingRateId, cancellationToken);
        if (rate is null)
            return Error.NotFound(description: $"Shipping rate '{command.ShippingRateId}' was not found.");

        _db.ShippingRates.Remove(rate);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
