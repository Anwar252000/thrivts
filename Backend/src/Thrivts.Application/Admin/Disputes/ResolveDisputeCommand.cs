using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Disputes;

public sealed record ResolveDisputeCommand(Guid DisputeId, string Resolution, string? ResolutionNotes, decimal RefundAmountUsd) : ICommand<ErrorOr<Success>>;

public sealed class ResolveDisputeCommandHandler : ICommandHandler<ResolveDisputeCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public ResolveDisputeCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(ResolveDisputeCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can resolve a dispute.");

        var dispute = await _db.Disputes.FirstOrDefaultAsync(d => d.Id == command.DisputeId, cancellationToken);
        if (dispute is null)
            return Error.NotFound(description: $"Dispute '{command.DisputeId}' was not found.");

        dispute.Resolve(command.Resolution, command.ResolutionNotes, command.RefundAmountUsd, _currentUser.UserId.Value, _clock.UtcNow);

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == dispute.DealId, cancellationToken);
        deal?.ClearDispute();

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
