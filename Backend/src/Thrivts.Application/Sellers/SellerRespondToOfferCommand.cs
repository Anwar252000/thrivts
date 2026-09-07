using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Application.Sellers;

/// <summary>Replaces accept_seller_offer / post_offer_round's seller-side calls — mirrors
/// submitOfferResponse()'s dispatch on action in seller.html. Accepting does NOT itself create a
/// deal (RequirementSellerOffer's own doc comment) — it only nudges the requirement to Matching, the
/// same as the live RPC.</summary>
public sealed record SellerRespondToOfferCommand(Guid OfferId, string Action, decimal? PricePerPcUsd, string? Notes) : ICommand<ErrorOr<Success>>;

public sealed class SellerRespondToOfferCommandHandler : ICommandHandler<SellerRespondToOfferCommand, ErrorOr<Success>>
{
    private static readonly OfferStatus[] TerminalStatuses = [OfferStatus.Accepted, OfferStatus.Declined, OfferStatus.Expired, OfferStatus.Withdrawn];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public SellerRespondToOfferCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(SellerRespondToOfferCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var sellerId = _currentUser.UserId.Value;

        var offer = await _db.RequirementSellerOffers.FirstOrDefaultAsync(o => o.Id == command.OfferId, cancellationToken);
        if (offer is null)
            return Error.NotFound(description: $"Offer '{command.OfferId}' was not found.");
        if (offer.SellerId != sellerId)
            return Error.Forbidden(description: "You can only respond to your own offers.");

        var now = _clock.UtcNow;

        try
        {
            switch (command.Action)
            {
                case "accept":
                    offer.Accept(now);
                    var requirement = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == offer.RequirementId, cancellationToken);
                    requirement?.MarkMatchingFromAcceptedOffer();
                    break;

                case "decline":
                    offer.Decline(now);
                    break;

                case "counter":
                    if (command.PricePerPcUsd is null || command.PricePerPcUsd <= 0)
                        return Error.Validation(description: "Enter a valid counter price.");
                    offer.PostRound(NegotiationActor.Seller, command.PricePerPcUsd.Value, command.Notes, now);
                    _db.OfferRounds.Add(new OfferRound(offer.Id, NegotiationActor.Seller, "counter", command.PricePerPcUsd, command.Notes, sellerId));
                    break;

                case "message":
                    if (string.IsNullOrWhiteSpace(command.Notes))
                        return Error.Validation(description: "Write a message first.");
                    if (TerminalStatuses.Contains(offer.Status))
                        return Error.Validation(description: $"This offer is closed ({offer.Status}) and cannot be negotiated.");
                    _db.OfferRounds.Add(new OfferRound(offer.Id, NegotiationActor.Seller, "message", null, command.Notes, sellerId));
                    break;

                default:
                    return Error.Validation(description: $"Unknown action: {command.Action}");
            }
        }
        catch (DomainException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
