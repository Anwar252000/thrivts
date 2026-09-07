using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Sellers;

/// <summary>
/// Replaces the seller_requirements_view read. The view itself is unfiltered by tier/tags — that
/// gating lived entirely in the requirements_seller_browse / sellers_read_eligible_requirements RLS
/// policies (confirmed by reading their pg_policies definitions directly against the live DB), so
/// this handler reimplements that gate explicitly: seller must be approved+active, requirement must
/// be Posted/Matching, seller.Tier must rank >= requirement.MinSellerTier (SellerTier's own enum
/// order already matches the live tier_rank() ordering), and RestrictedToTags — when non-empty —
/// must overlap the seller's own Tags.
/// </summary>
public sealed record GetOpenRequirementsForSellerQuery : IQuery<ErrorOr<List<OpenRequirementDto>>>;

public sealed record OpenRequirementDto(
    Guid Id, string RequirementNumber, string ItemName, int QuantityPcs, GradeType Grade, string DestinationCountry,
    string? ShippingMode, int? DeliveryTimelineDays, RequirementStatus Status, DateTimeOffset? PostedAt, bool HasQuoted);

public sealed class GetOpenRequirementsForSellerQueryHandler : IQueryHandler<GetOpenRequirementsForSellerQuery, ErrorOr<List<OpenRequirementDto>>>
{
    private static readonly RequirementStatus[] OpenStatuses = [RequirementStatus.Posted, RequirementStatus.Matching];

    // Mirrors the live tier_rank() function exactly rather than trusting native Postgres enum
    // ordering — tier_rank() exists specifically because the DB doesn't rely on declaration-order
    // comparison for this, so this filter is applied client-side in C# after the DB round-trip
    // instead of translating a `<=` comparison on the enum column.
    private static readonly Dictionary<SellerTier, int> TierRank = new()
    {
        [SellerTier.Bronze] = 1,
        [SellerTier.Silver] = 2,
        [SellerTier.Gold] = 3,
        [SellerTier.Platinum] = 4,
    };

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOpenRequirementsForSellerQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<OpenRequirementDto>>> Handle(GetOpenRequirementsForSellerQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var sellerId = _currentUser.UserId.Value;

        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == sellerId, cancellationToken);
        if (profile is null || profile.ApprovalStatus != ApprovalStatus.Approved || !profile.IsActive)
            return Error.Forbidden(description: "Only approved sellers can view open requirements.");

        var seller = await _db.Sellers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sellerId, cancellationToken);
        if (seller is null)
            return Error.NotFound(description: "Seller profile was not found.");

        var quotedRequirementIds = await _db.SellerResponses.AsNoTracking()
            .Where(sr => sr.SellerId == sellerId)
            .Select(sr => sr.RequirementId)
            .ToListAsync(cancellationToken);
        var quotedSet = quotedRequirementIds.ToHashSet();

        var requirements = await _db.Requirements.AsNoTracking()
            .Where(r => OpenStatuses.Contains(r.Status))
            .OrderByDescending(r => r.PostedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        var sellerRank = TierRank[seller.Tier];
        var eligible = requirements
            .Where(r => sellerRank >= TierRank[r.MinSellerTier])
            .Where(r => r.RestrictedToTags is null || r.RestrictedToTags.Length == 0 || (seller.Tags?.Intersect(r.RestrictedToTags).Any() ?? false))
            .Select(r => new OpenRequirementDto(
                r.Id, r.RequirementNumber, r.ItemName, r.QuantityPcs, r.Grade, r.DestinationCountry,
                r.ShippingMode, r.DeliveryTimelineDays, r.Status, r.PostedAt, quotedSet.Contains(r.Id)))
            .ToList();

        return eligible;
    }
}
