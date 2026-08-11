using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Agencies;

/// <summary>
/// Replaces admin_create_agency. UserId must be an EXISTING Supabase Auth user id (today the
/// admin creates that login via the Supabase dashboard first and pastes the id in) — this command
/// creates the Profile + Agency rows for it, it does not create the login itself. CommissionRate
/// is a PERCENTAGE (0-100), matching Agency.CommissionRate's storage convention.
/// </summary>
public sealed record CreateAgencyCommand(
    Guid UserId,
    string Email,
    string AgencyName,
    string OwnerFullName,
    string Country,
    string? City,
    string? Phone,
    string? WhatsApp,
    decimal CommissionRate,
    int? TeamSize,
    string? Notes) : ICommand<ErrorOr<Guid>>;

public sealed class CreateAgencyCommandHandler : ICommandHandler<CreateAgencyCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateAgencyCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(CreateAgencyCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can create an agency.");

        if (await _db.Profiles.AnyAsync(p => p.Id == command.UserId, cancellationToken))
            return Error.Validation(description: "This user already has a profile.");

        var profile = new Profile(command.UserId, UserRole.Agency, command.Email, command.OwnerFullName);
        profile.Approve(_currentUser.UserId!.Value, DateTimeOffset.UtcNow);

        var agency = new Agency(command.UserId, command.AgencyName, GenerateAgencyCode(command.AgencyName), command.OwnerFullName, command.Country);
        agency.SetCommissionRate(command.CommissionRate);

        _db.Profiles.Add(profile);
        _db.Agencies.Add(agency);
        await _db.SaveChangesAsync(cancellationToken);

        return agency.Id;
    }

    private static string GenerateAgencyCode(string agencyName)
    {
        var slug = new string(agencyName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var prefix = slug.Length >= 4 ? slug[..4] : slug.PadRight(4, 'X');
        return $"{prefix}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
    }
}
