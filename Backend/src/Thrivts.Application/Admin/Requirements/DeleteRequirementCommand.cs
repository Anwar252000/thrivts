using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>
/// Replaces admin_delete_requirement. If the requirement has linked deals, Confirm must be true
/// (the client is expected to make the admin type "DELETE" first, matching admin.html's
/// deleteRequirementAdmin() danger-confirm) — deleting then CASCADES: removes the deal(s) and
/// their allocations, commissions, and disputes. This is destructive and irreversible.
/// </summary>
public sealed record DeleteRequirementCommand(Guid RequirementId, bool Confirm) : ICommand<ErrorOr<Success>>;

public sealed class DeleteRequirementCommandHandler : ICommandHandler<DeleteRequirementCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteRequirementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Success>> Handle(DeleteRequirementCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can delete a requirement.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        var deals = await _db.Deals.Where(d => d.RequirementId == command.RequirementId).ToListAsync(cancellationToken);
        if (deals.Count > 0 && !command.Confirm)
            return Error.Validation(description: $"This requirement has {deals.Count} linked deal(s). Set Confirm=true to permanently delete the requirement, its deal(s), and their allocations, commissions, and disputes.");

        foreach (var deal in deals)
        {
            var allocations = _db.DealAllocations.Where(a => a.DealId == deal.Id);
            _db.DealAllocations.RemoveRange(allocations);

            var commissions = _db.Commissions.Where(c => c.DealId == deal.Id);
            _db.Commissions.RemoveRange(commissions);

            var influencerCommissions = _db.InfluencerCommissions.Where(c => c.DealId == deal.Id);
            _db.InfluencerCommissions.RemoveRange(influencerCommissions);

            var disputes = _db.Disputes.Where(d => d.DealId == deal.Id);
            _db.Disputes.RemoveRange(disputes);
        }
        _db.Deals.RemoveRange(deals);

        var offers = _db.RequirementSellerOffers.Where(o => o.RequirementId == command.RequirementId);
        _db.RequirementSellerOffers.RemoveRange(offers);

        var bids = _db.SellerResponses.Where(sr => sr.RequirementId == command.RequirementId);
        _db.SellerResponses.RemoveRange(bids);

        _db.Requirements.Remove(requirement);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
