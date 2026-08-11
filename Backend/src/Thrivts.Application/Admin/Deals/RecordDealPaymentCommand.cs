using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

public sealed record RecordDealPaymentCommand(Guid DealId, string PaymentMethod, string? PaymentReference) : ICommand<ErrorOr<Success>>;

public sealed class RecordDealPaymentCommandHandler : ICommandHandler<RecordDealPaymentCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public RecordDealPaymentCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(RecordDealPaymentCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can record a deal payment.");

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        deal.RecordPayment(command.PaymentMethod, command.PaymentReference, _currentUser.UserId.Value, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
