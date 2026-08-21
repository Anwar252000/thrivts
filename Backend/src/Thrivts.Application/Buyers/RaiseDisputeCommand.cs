using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces buyer.html's submitDispute() — raises a dispute against one of the caller's
/// own deals.</summary>
public sealed record RaiseDisputeCommand(Guid DealId, string Description, string? Category, string? RequestedResolution)
    : ICommand<ErrorOr<Guid>>;

public sealed class RaiseDisputeCommandHandler : ICommandHandler<RaiseDisputeCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RaiseDisputeCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(RaiseDisputeCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");
        if (deal.BuyerId != _currentUser.UserId)
            return Error.Forbidden(description: "You can only raise an issue on your own deals.");

        var disputeNumber = await NextDisputeNumberAsync(cancellationToken);
        var dispute = new Dispute(disputeNumber, command.DealId, _currentUser.UserId.Value, command.Description, command.Category, command.RequestedResolution);

        _db.Disputes.Add(dispute);
        await _db.SaveChangesAsync(cancellationToken);

        return dispute.Id;
    }

    private async Task<string> NextDisputeNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"DSP-{year}-";
        var count = await _db.Disputes.CountAsync(d => d.DisputeNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D5}";
    }
}
