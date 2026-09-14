using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

/// <summary>
/// Replaces admin_delete_buyer — read in full from the live DB. That function cascades deletes
/// across deal/requirement children, buyer-owned rows, and rows where the buyer acted (messages,
/// disputes raised), then nulls out ~10 "who did it" FK columns elsewhere so the profile delete
/// isn't blocked, before finally deleting buyers then profiles. It skips a legacy `bids` table our
/// schema never had (wrapped in an exception-swallowing block in the source, confirming it's dead
/// even there). Order matters: children of deals/requirements first, then the owning rows, then the
/// FK references pointing at this buyer, then the buyer/profile rows themselves.
/// </summary>
public sealed record DeleteBuyerCommand(Guid BuyerId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteBuyerCommandHandler : ICommandHandler<DeleteBuyerCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteBuyerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteBuyerCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can delete a buyer.");

        var buyerId = command.BuyerId;

        var buyerExists = await _db.Buyers.AnyAsync(b => b.Id == buyerId, cancellationToken);
        if (!buyerExists)
            return Error.NotFound(description: $"Buyer '{buyerId}' was not found.");

        var dealIds = await _db.Deals.Where(d => d.BuyerId == buyerId).Select(d => d.Id).ToListAsync(cancellationToken);
        var requirementIds = await _db.Requirements.Where(r => r.BuyerId == buyerId).Select(r => r.Id).ToListAsync(cancellationToken);

        // Children of the buyer's deals.
        await _db.PurchaseOrders.Where(po => dealIds.Contains(po.DealId)).ExecuteDeleteAsync(cancellationToken);
        await _db.DealAllocations.Where(a => dealIds.Contains(a.DealId)).ExecuteDeleteAsync(cancellationToken);
        await _db.Disputes.Where(d => dealIds.Contains(d.DealId)).ExecuteDeleteAsync(cancellationToken);

        // Buyer-owned rows.
        await _db.InfluencerCommissions.Where(c => c.BuyerId == buyerId).ExecuteDeleteAsync(cancellationToken);
        await _db.Deals.Where(d => d.BuyerId == buyerId).ExecuteDeleteAsync(cancellationToken);
        await _db.Requirements.Where(r => r.BuyerId == buyerId).ExecuteDeleteAsync(cancellationToken);

        // Rows where the buyer is the actor.
        await _db.Notifications.Where(n => n.RecipientId == buyerId).ExecuteDeleteAsync(cancellationToken);
        await _db.Messages.Where(m => m.SenderId == buyerId).ExecuteDeleteAsync(cancellationToken);
        await _db.MessageThreads.Where(t => t.ParticipantId == buyerId).ExecuteDeleteAsync(cancellationToken);
        await _db.Disputes.Where(d => d.RaisedBy == buyerId).ExecuteDeleteAsync(cancellationToken);

        // Null out "who did it" references so the profile delete isn't FK-blocked.
        await _db.Deals.Where(d => d.PaymentReceivedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(d => d.PaymentReceivedBy, (Guid?)null), cancellationToken);
        await _db.PurchaseOrders.Where(po => po.IssuedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(po => po.IssuedBy, (Guid?)null), cancellationToken);
        await _db.PurchaseOrders.Where(po => po.VerifiedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(po => po.VerifiedBy, (Guid?)null), cancellationToken);
        await _db.Disputes.Where(d => d.ResolvedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(d => d.ResolvedBy, (Guid?)null), cancellationToken);
        await _db.Commissions.Where(c => c.ReleasedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(c => c.ReleasedBy, (Guid?)null), cancellationToken);
        await _db.ExchangeRates.Where(e => e.SetBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(e => e.SetBy, (Guid?)null), cancellationToken);
        await _db.Profiles.Where(p => p.ApprovedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(p => p.ApprovedBy, (Guid?)null), cancellationToken);
        await _db.Buyers.Where(b => b.AttributedToAgency == buyerId).ExecuteUpdateAsync(s => s.SetProperty(b => b.AttributedToAgency, (Guid?)null), cancellationToken);
        await _db.SellerInvites.Where(i => i.InvitedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(i => i.InvitedBy, (Guid?)null), cancellationToken);
        await _db.SellerInvites.Where(i => i.UsedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedBy, (Guid?)null), cancellationToken);
        await _db.AuditLogs.Where(a => a.ActorId == buyerId).ExecuteUpdateAsync(s => s.SetProperty(a => a.ActorId, (Guid?)null), cancellationToken);
        await _db.BlockedIps.Where(b => b.BlockedBy == buyerId).ExecuteUpdateAsync(s => s.SetProperty(b => b.BlockedBy, (Guid?)null), cancellationToken);

        // Finally the buyer + profile.
        await _db.Buyers.Where(b => b.Id == buyerId).ExecuteDeleteAsync(cancellationToken);
        await _db.Profiles.Where(p => p.Id == buyerId).ExecuteDeleteAsync(cancellationToken);

        return Result.Success;
    }
}
