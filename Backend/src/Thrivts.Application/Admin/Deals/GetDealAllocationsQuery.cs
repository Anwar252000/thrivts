using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

/// <summary>
/// Admin-only — includes the seller's real identity (CompanyName, SellerCode, Phone, WhatsApp),
/// unlike any buyer-facing projection. This is exactly the de-anonymized view
/// admin_reveal_seller() used to gate; never reuse this DTO shape on a non-admin endpoint.
/// </summary>
public sealed record GetDealAllocationsQuery(Guid DealId) : IQuery<ErrorOr<List<DealAllocationDto>>>;

public sealed record DealAllocationDto(
    Guid Id, Guid SellerId, string? SellerCompanyName, string? SellerCode, string? SellerPhone, string? SellerWhatsApp,
    string LotNumber, int AllocatedQuantityPcs, decimal PricePerPcUsd, decimal TotalPayoutUsd,
    BidStatus Status, DateTimeOffset? PayoutPaidAt);

public sealed class GetDealAllocationsQueryHandler : IQueryHandler<GetDealAllocationsQuery, ErrorOr<List<DealAllocationDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDealAllocationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<DealAllocationDto>>> Handle(GetDealAllocationsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view deal allocations.");

        var result = await (
                from allocation in _db.DealAllocations.AsNoTracking()
                where allocation.DealId == query.DealId
                join seller in _db.Sellers.AsNoTracking() on allocation.SellerId equals seller.Id into sellers
                from seller in sellers.DefaultIfEmpty()
                select new DealAllocationDto(
                    allocation.Id, allocation.SellerId, seller.CompanyName, seller.SellerCode, seller.Phone, seller.WhatsApp,
                    allocation.LotNumber, allocation.AllocatedQuantityPcs, allocation.PricePerPcUsd, allocation.TotalPayoutUsd,
                    allocation.Status, allocation.PayoutPaidAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
