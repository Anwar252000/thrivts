using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Sellers;

/// <summary>Replaces loadOffers()'s pair of reads (requirement_seller_offers + offer_rounds) —
/// bundled into one call since the seller UI always renders them together (renderOfferThread()).</summary>
public sealed record GetSellerOffersQuery : IQuery<ErrorOr<List<SellerOfferDto>>>;

public sealed record SellerOfferDto(
    Guid Id, string? OfferNumber, Guid RequirementId, string? ItemName, int? QuantityPcs, string? Grade,
    decimal? OfferPricePerPc, decimal? CurrentPricePerPc, OfferStatus Status, string? AdminNotes,
    DateTimeOffset? SentAt, DateTimeOffset? ExpiresAt, List<OfferRoundDto> Rounds);

public sealed record OfferRoundDto(Guid Id, NegotiationActor Party, string Kind, decimal? PricePerPcUsd, string? Notes, DateTimeOffset CreatedAt);

public sealed class GetSellerOffersQueryHandler : IQueryHandler<GetSellerOffersQuery, ErrorOr<List<SellerOfferDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSellerOffersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<SellerOfferDto>>> Handle(GetSellerOffersQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var sellerId = _currentUser.UserId.Value;

        var offers = await _db.RequirementSellerOffers.AsNoTracking()
            .Where(o => o.SellerId == sellerId)
            .OrderByDescending(o => o.SentAt)
            .ToListAsync(cancellationToken);

        var offerIds = offers.Select(o => o.Id).ToArray();
        var roundsByOfferId = (await _db.OfferRounds.AsNoTracking()
                .Where(r => offerIds.Contains(r.OfferId))
                .OrderBy(r => r.CreatedAt)
                .ToListAsync(cancellationToken))
            .GroupBy(r => r.OfferId)
            .ToDictionary(g => g.Key, g => g.Select(r => new OfferRoundDto(r.Id, r.Party, r.Kind, r.PricePerPcUsd, r.Notes, r.CreatedAt)).ToList());

        var result = offers.Select(o => new SellerOfferDto(
            o.Id, o.OfferNumber, o.RequirementId, o.ItemName, o.QuantityPcs, o.Grade,
            o.OfferPricePerPc, o.CurrentPricePerPc, o.Status, o.AdminNotes, o.SentAt, o.ExpiresAt,
            roundsByOfferId.GetValueOrDefault(o.Id, []))).ToList();

        return result;
    }
}
