using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.DomainServices;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Admin.Deals;

/// <summary>
/// Replaces the old advance_deal_status RPC. Cancelling is NOT handled here — see
/// CancelDealCommand, which needs a reason and runs the cancel cascade; the live admin.html keeps
/// them as separate flows too (submitAdvance vs cancelDeal).
///
/// Closes THRIVTS_FLOW_MATRIX.md §4.3 (open bug #3): on Delivered, an agency commission is
/// accrued (release due = delivered + 20 days, per CommissionCalculator.CommissionReleaseDelay);
/// on Settled, both the agency commission and any influencer commission are released, and the
/// requirement is marked settled. Matches admin.html's "This releases the agency commission" copy
/// on its own Settle confirmation.
/// </summary>
public sealed class AdvanceDealStatusCommandHandler : ICommandHandler<AdvanceDealStatusCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AdvanceDealStatusCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(AdvanceDealStatusCommand command, CancellationToken cancellationToken)
    {
        // Controller-level [Authorize(Policy = "AdminOnly")] is the first gate; re-check here too —
        // never trust that only the admin UI can reach this handler.
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can advance a deal's status.");

        if (command.NewStatus == DealStatus.Cancelled)
            return Error.Validation(description: "Use the cancel-deal endpoint to cancel a deal (it requires a reason).");

        var deal = await _db.Deals.FirstOrDefaultAsync(d => d.Id == command.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{command.DealId}' was not found.");

        if (command.NewStatus == DealStatus.Dispatched && string.IsNullOrWhiteSpace(deal.TrackingNumber))
            return Error.Validation(description: "Set tracking details (tracking number + courier) before marking a deal dispatched.");

        var now = _clock.UtcNow;

        try
        {
            deal.AdvanceTo(command.NewStatus, now);
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        if (command.NewStatus == DealStatus.Delivered && deal.AgencyId is not null)
        {
            var existingCommission = await _db.Commissions.FirstOrDefaultAsync(c => c.DealId == deal.Id, cancellationToken);
            if (existingCommission is null)
            {
                var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.Id == deal.AgencyId, cancellationToken);
                var rate = agency?.CommissionRate ?? 30.00m;
                var commissionAmount = deal.TotalSpreadUsd * (rate / 100m);
                var releaseDueAt = now + CommissionCalculator.CommissionReleaseDelay;

                var commission = new Commission(deal.Id, deal.AgencyId.Value, rate, deal.TotalSpreadUsd, commissionAmount, releaseDueAt);
                commission.Accrue(now, releaseDueAt);
                _db.Commissions.Add(commission);
            }
        }

        if (command.NewStatus == DealStatus.Settled)
        {
            var commission = await _db.Commissions.FirstOrDefaultAsync(c => c.DealId == deal.Id, cancellationToken);
            commission?.Release(now, _currentUser.UserId ?? Guid.Empty, commission.CommissionAmountUsd, deal.TotalSpreadUsd);

            var influencerCommission = await _db.InfluencerCommissions.FirstOrDefaultAsync(c => c.DealId == deal.Id, cancellationToken);
            influencerCommission?.Release(now);

            var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == deal.RequirementId, cancellationToken);
            requirement?.MarkSettled(now);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
