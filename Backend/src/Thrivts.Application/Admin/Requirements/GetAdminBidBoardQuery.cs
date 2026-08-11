using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>
/// Replaces get_admin_bid_board / the admin_bid_board view — the ONLY place seller real identity
/// is joined to a bid; admin-only, never reuse this DTO shape on a buyer-facing endpoint.
/// </summary>
public sealed record GetAdminBidBoardQuery(Guid? RequirementId) : IQuery<ErrorOr<List<AdminBidBoardRowDto>>>;

public sealed record AdminBidBoardRowDto(
    Guid RequirementId, string RequirementNumber, string ItemName, int RequiredPcs, RequirementStatus RequirementStatus,
    Guid BidId, int BidPcs, decimal? SellerPricePerPc, decimal? FeePerPc, decimal? BuyerSeesPerPc, decimal? BuyerTotal,
    BidStatus BidStatus, DateTimeOffset? RespondedAt,
    Guid SellerId, string? SellerCompanyName, string SellerAlias, SellerTier SellerTier, bool KycVerified);

public sealed class GetAdminBidBoardQueryHandler : IQueryHandler<GetAdminBidBoardQuery, ErrorOr<List<AdminBidBoardRowDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAdminBidBoardQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<AdminBidBoardRowDto>>> Handle(GetAdminBidBoardQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view the bid board.");

        var bids = _db.SellerResponses.AsNoTracking().AsQueryable();
        if (query.RequirementId is not null)
            bids = bids.Where(b => b.RequirementId == query.RequirementId);

        var result = await (
            from bid in bids
            join requirement in _db.Requirements.AsNoTracking() on bid.RequirementId equals requirement.Id
            join seller in _db.Sellers.AsNoTracking() on bid.SellerId equals seller.Id
            orderby bid.RespondedAt descending
            select new AdminBidBoardRowDto(
                requirement.Id, requirement.RequirementNumber, requirement.ItemName, requirement.QuantityPcs, requirement.Status,
                bid.Id, bid.AvailableQuantityPcs, bid.CurrentPriceUsd, bid.FeePerPcAppliedUsd,
                bid.CurrentPriceUsd + bid.FeePerPcAppliedUsd,
                (bid.CurrentPriceUsd + bid.FeePerPcAppliedUsd) * bid.AvailableQuantityPcs,
                bid.Status, bid.RespondedAt,
                seller.Id, seller.CompanyName, seller.PublicAlias, seller.Tier, seller.KycVerified))
            .ToListAsync(cancellationToken);

        return result;
    }
}
