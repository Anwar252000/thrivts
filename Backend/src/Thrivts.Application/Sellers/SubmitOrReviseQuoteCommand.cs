using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Sellers;

/// <summary>Replaces submitQuote()'s two paths — a fresh bid (client insert against
/// seller_responses in the old system) and seller_revise_bid (updating a bid the seller already
/// owns on this requirement). One requirement can only ever hold one bid per seller
/// (seller_responses_requirement_id_seller_id_key), so this dispatches on whether one already exists
/// rather than exposing two separate endpoints.</summary>
public sealed record SubmitOrReviseQuoteCommand(Guid RequirementId, int AvailableQuantityPcs, decimal PricePerPcUsd, string? SellerNotes)
    : ICommand<ErrorOr<Guid>>;

public sealed class SubmitOrReviseQuoteCommandHandler : ICommandHandler<SubmitOrReviseQuoteCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SubmitOrReviseQuoteCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(SubmitOrReviseQuoteCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var sellerId = _currentUser.UserId.Value;

        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == sellerId, cancellationToken);
        if (profile is null || profile.ApprovalStatus != ApprovalStatus.Approved || !profile.IsActive)
            return Error.Forbidden(description: "Only approved sellers can submit quotes.");

        var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == command.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{command.RequirementId}' was not found.");

        var existing = await _db.SellerResponses
            .FirstOrDefaultAsync(sr => sr.RequirementId == command.RequirementId && sr.SellerId == sellerId, cancellationToken);

        try
        {
            if (existing is not null)
            {
                existing.Revise(command.PricePerPcUsd, command.AvailableQuantityPcs, command.SellerNotes);

                // Mirrors seller_revise_bid's perform push_notification(...) call — a revise is an
                // UPDATE, not an INSERT, so trg_notify_new_bid (which only fires on INSERT) never
                // covers this path; it has to be written explicitly here.
                var alias = await _db.Sellers.AsNoTracking().Where(s => s.Id == sellerId).Select(s => s.PublicAlias).FirstOrDefaultAsync(cancellationToken);
                _db.Notifications.Add(new Notification(
                    requirement.BuyerId, $"Updated bid on {requirement.RequirementNumber}",
                    $"{alias ?? "A seller"} revised their quote on {command.AvailableQuantityPcs} pcs",
                    refType: "requirement", refId: command.RequirementId));

                await _db.SaveChangesAsync(cancellationToken);
                return existing.Id;
            }

            var feePerPc = await ResolveFeePerPcAsync(sellerId, cancellationToken);
            var bid = new SellerResponse(command.RequirementId, sellerId, command.AvailableQuantityPcs, command.PricePerPcUsd, feePerPc, command.SellerNotes);
            _db.SellerResponses.Add(bid);
            await _db.SaveChangesAsync(cancellationToken);
            return bid.Id;
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }
    }

    /// <summary>Mirrors effective_fee_per_pc(): a seller-scoped PlatformFeeOverride wins over the
    /// global PlatformFeeConfig. Category-scoped overrides are skipped — PlatformFeeOverride.CategoryId
    /// is a live-schema uuid while Category.Id is an int, so the two can never actually correlate
    /// (see PlatformFeeOverride's own doc comment).</summary>
    private async Task<decimal> ResolveFeePerPcAsync(Guid sellerId, CancellationToken cancellationToken)
    {
        var sellerOverride = await _db.PlatformFeeOverrides.AsNoTracking()
            .Where(o => o.SellerId == sellerId)
            .Select(o => (decimal?)o.FeePerPcUsd)
            .FirstOrDefaultAsync(cancellationToken);
        if (sellerOverride is not null)
            return sellerOverride.Value;

        var config = await _db.PlatformFeeConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.Id, cancellationToken);
        return config?.FeePerPcUsd ?? 0.70m;
    }
}
