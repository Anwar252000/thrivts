using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces the buyer_deals_view read — powers both "My deals" and (client-side filtered
/// to Settled) the "Order history" section. Shape mirrors buyer.html's mapBuyerDeal() so the
/// eventual frontend needs no reshaping.</summary>
public sealed record GetMyDealsQuery : IQuery<ErrorOr<List<MyDealListItemDto>>>;

public sealed record MyDealListItemDto(
    Guid Id, string DealNumber, string RequirementNumber, string ItemName, GradeType Grade, string DestinationCountry,
    string? ShippingMode, int TotalQuantityPcs, decimal TotalInvoiceUsd, DealStatus Status, bool HasDispute, DateTimeOffset CreatedAt);

public sealed class GetMyDealsQueryHandler : IQueryHandler<GetMyDealsQuery, ErrorOr<List<MyDealListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyDealsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<MyDealListItemDto>>> Handle(GetMyDealsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var result = await (
            from d in _db.Deals.AsNoTracking()
            join r in _db.Requirements.AsNoTracking() on d.RequirementId equals r.Id
            where d.BuyerId == _currentUser.UserId
            orderby d.CreatedAt descending
            select new MyDealListItemDto(
                d.Id, d.DealNumber, r.RequirementNumber, r.ItemName, r.Grade, r.DestinationCountry, r.ShippingMode,
                d.TotalQuantityPcs, d.TotalInvoiceUsd, d.Status, d.HasDispute, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
