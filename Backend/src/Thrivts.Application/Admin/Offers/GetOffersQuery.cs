using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

public sealed record GetOffersQuery(Guid? RequirementId, OfferStatus? Status) : IQuery<ErrorOr<List<OfferListItemDto>>>;

public sealed record OfferListItemDto(
    Guid Id, string? OfferNumber, Guid RequirementId, Guid SellerId, string? ItemName, int? QuantityPcs, string? Grade,
    decimal? OfferPricePerPc, decimal? CurrentPricePerPc, OfferStatus Status, string? AdminNotes, Guid? DealId,
    DateTimeOffset? SentAt, DateTimeOffset? ExpiresAt);

public sealed class GetOffersQueryHandler : IQueryHandler<GetOffersQuery, ErrorOr<List<OfferListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOffersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<OfferListItemDto>>> Handle(GetOffersQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list offers.");

        var offers = _db.RequirementSellerOffers.AsNoTracking();
        if (query.RequirementId is not null)
            offers = offers.Where(o => o.RequirementId == query.RequirementId);
        if (query.Status is not null)
            offers = offers.Where(o => o.Status == query.Status);

        var result = await offers
            .OrderByDescending(o => o.SentAt)
            .Select(o => new OfferListItemDto(
                o.Id, o.OfferNumber, o.RequirementId, o.SellerId, o.ItemName, o.QuantityPcs, o.Grade,
                o.OfferPricePerPc, o.CurrentPricePerPc, o.Status, o.AdminNotes, o.DealId, o.SentAt, o.ExpiresAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
