using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Offers;

public sealed record GetOfferRoundsQuery(Guid OfferId) : IQuery<ErrorOr<List<OfferRoundDto>>>;

public sealed record OfferRoundDto(Guid Id, NegotiationActor Party, string Kind, decimal? PricePerPcUsd, string? Notes, DateTimeOffset CreatedAt);

public sealed class GetOfferRoundsQueryHandler : IQueryHandler<GetOfferRoundsQuery, ErrorOr<List<OfferRoundDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetOfferRoundsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<OfferRoundDto>>> Handle(GetOfferRoundsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view offer round history.");

        var result = await _db.OfferRounds.AsNoTracking()
            .Where(r => r.OfferId == query.OfferId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new OfferRoundDto(r.Id, r.Party, r.Kind, r.PricePerPcUsd, r.Notes, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
