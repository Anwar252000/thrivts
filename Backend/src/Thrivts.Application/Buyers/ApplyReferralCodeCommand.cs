using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces apply_referral_code — lets an ALREADY-registered buyer link a partner/referral
/// code retroactively (buyer.html retries this on every session load via applyPendingReferral(),
/// for a buyer who clicks a referral link after already having an account; the signup-time linking
/// in RegisterBuyerCommand covers the case where the code is entered on the application form
/// itself). Only linkable once, and only before the buyer's first order — matches the "5% off your
/// first order" partner-code offer.</summary>
public sealed record ApplyReferralCodeCommand(string Code) : ICommand<ErrorOr<Success>>;

public sealed class ApplyReferralCodeCommandHandler : ICommandHandler<ApplyReferralCodeCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public ApplyReferralCodeCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(ApplyReferralCodeCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var buyer = await _db.Buyers.FirstOrDefaultAsync(b => b.Id == _currentUser.UserId, cancellationToken);
        if (buyer is null)
            return Error.NotFound(description: "Buyer profile was not found.");
        if (buyer.InfluencerId is not null)
            return Error.Validation(description: "A partner code is already linked to your account.");
        if (buyer.FirstOrderAt is not null)
            return Error.Validation(description: "Partner codes can only be applied before your first order.");

        var code = command.Code.ToLower();
        var influencer = await _db.Influencers.FirstOrDefaultAsync(i => i.ReferralCode.ToLower() == code, cancellationToken);
        if (influencer is null)
            return Error.Validation(description: "That partner code isn't valid.");

        buyer.LinkReferral(influencer.Id, command.Code, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
