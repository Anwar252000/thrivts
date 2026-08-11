using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Influencers;

/// <summary>
/// Replaces admin_create_influencer. UserId is optional (unlike Agency, an influencer doesn't
/// need a login). CommissionRate is a FRACTION (0-1), matching Influencer.CommissionRate's
/// storage convention (DB default 0.05 = 5%).
/// </summary>
public sealed record CreateInfluencerCommand(
    string FullName,
    string Email,
    string? Phone,
    string? Instagram,
    string? Tiktok,
    string? ReferralCode,
    decimal CommissionRate,
    Guid? UserId) : ICommand<ErrorOr<Guid>>;

public sealed class CreateInfluencerCommandHandler : ICommandHandler<CreateInfluencerCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateInfluencerCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(CreateInfluencerCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can create an influencer.");

        var referralCode = command.ReferralCode ?? GenerateReferralCode(command.FullName);
        var influencerCode = $"INF-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

        var influencer = new Influencer(influencerCode, referralCode, command.UserId, command.FullName);
        influencer.SetCommissionRate(command.CommissionRate);
        influencer.UpdateContactDetails(command.Email, command.Phone, command.Instagram, command.Tiktok);

        _db.Influencers.Add(influencer);
        await _db.SaveChangesAsync(cancellationToken);

        return influencer.Id;
    }

    private static string GenerateReferralCode(string fullName)
    {
        var slug = new string(fullName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var prefix = slug.Length >= 4 ? slug[..4] : slug.PadRight(4, 'X');
        return $"{prefix}{Random.Shared.Next(100, 999)}";
    }
}
