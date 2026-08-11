using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Admin.Deals;

/// <summary>
/// Replaces advance_deal_status('cancelled') — kept separate from AdvanceDealStatusCommand
/// because cancelling needs a reason and a cascade the forward path doesn't: frees the source
/// bid back onto the board, reverts the requirement to Matching if nothing else is committed, and
/// cancels/reverses any accrued commissions. Mirrors fixes_applied/11_FIX_cancel_cascade.sql.
/// </summary>
public sealed record CancelDealCommand(Guid DealId, string Reason) : ICommand<ErrorOr<Success>>;

public sealed class CancelDealCommandHandler : ICommandHandler<CancelDealCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public CancelDealCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(CancelDealCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can cancel a deal.");

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        var now = _clock.UtcNow;

        try
        {
            deal.Cancel(command.Reason, now);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        // 1) Free the source bid so the seller stops seeing "Deal confirmed" and the quantity releases.
        if (deal.SourceResponseId is not null)
        {
            var bid = await _db.SellerResponses.FirstOrDefaultAsync(sr => sr.Id == deal.SourceResponseId, cancellationToken);
            bid?.ReleaseFromCancelledDeal();
        }

        // 2) Revert the requirement if nothing else is still committed.
        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == deal.RequirementId, cancellationToken);
        if (requirement is not null)
        {
            var stillCommitted = await _db.SellerResponses
                .AnyAsync(sr => sr.RequirementId == deal.RequirementId && sr.Status == BidStatus.Accepted, cancellationToken);
            if (!stillCommitted)
                requirement.RevertToMatching();
        }

        // 3) Reverse any commission accrual for this deal.
        var commission = await _db.Commissions.FirstOrDefaultAsync(c => c.DealId == deal.Id, cancellationToken);
        commission?.Cancel();

        var influencerCommission = await _db.InfluencerCommissions.FirstOrDefaultAsync(c => c.DealId == deal.Id, cancellationToken);
        influencerCommission?.Reverse();

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
