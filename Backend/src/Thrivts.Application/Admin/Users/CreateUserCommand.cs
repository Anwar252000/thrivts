using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Users;

/// <summary>
/// Admin-initiated account creation — the live system has no equivalent (every account there is
/// self-registered and then approved), so this is a new capability: it creates the Supabase Auth
/// identity directly via ISupabaseAdminClient (service_role key, email pre-confirmed — no
/// verification email), then the Profile + role-specific row, auto-approved since an admin is
/// vouching for it directly. If the DB-side writes fail after the auth user was created, the auth
/// user is deleted so a retry doesn't collide on a duplicate email.
/// </summary>
public sealed record CreateUserCommand(
    string Email, string Password, string FullName, UserRole Role, string? Phone, string? WhatsApp,
    // Buyer
    string? CompanyName, string? Country,
    // Seller
    string? PublicAlias, string? LocationCity, string? LocationCountry,
    // Agency
    string? AgencyName, decimal? CommissionRate) : ICommand<ErrorOr<Guid>>;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISupabaseAdminClient _supabaseAdmin;

    public CreateUserCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, ISupabaseAdminClient supabaseAdmin)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _supabaseAdmin = supabaseAdmin;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin || _currentUser.UserId is null)
            return Error.Forbidden(description: "Only admins can create a user.");

        Guid authUserId;
        try
        {
            authUserId = await _supabaseAdmin.CreateUserAsync(command.Email, command.Password, cancellationToken);
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Validation(description: ex.Message);
        }

        try
        {
            var now = _clock.UtcNow;
            var profile = new Profile(authUserId, command.Role, command.Email, command.FullName);
            profile.Approve(_currentUser.UserId.Value, now);
            profile.UpdateContactDetails(command.Phone, command.WhatsApp);
            _db.Profiles.Add(profile);

            switch (command.Role)
            {
                case UserRole.Buyer:
                    _db.Buyers.Add(new Buyer(authUserId, command.CompanyName ?? command.FullName, command.Country ?? "Unknown"));
                    break;
                case UserRole.Seller:
                    _db.Sellers.Add(new Seller(
                        authUserId,
                        command.PublicAlias ?? GenerateSellerAlias(),
                        command.LocationCity ?? "Unknown",
                        categoriesSupplied: [],
                        locationCountry: command.LocationCountry ?? "Pakistan"));
                    break;
                case UserRole.Agency:
                    var agency = new Agency(authUserId, command.AgencyName ?? command.FullName, GenerateAgencyCode(command.AgencyName ?? command.FullName), command.FullName, command.Country ?? "Unknown");
                    if (command.CommissionRate is not null)
                        agency.SetCommissionRate(command.CommissionRate.Value);
                    _db.Agencies.Add(agency);
                    break;
                case UserRole.Admin:
                    break;
            }

            await _db.SaveChangesAsync(cancellationToken);
            return authUserId;
        }
        catch
        {
            // Best-effort compensation — an orphaned Supabase Auth identity with no Profile row
            // can never sign into anything, but leaving it behind would block re-creating the
            // same email later.
            try { await _supabaseAdmin.DeleteUserAsync(authUserId, cancellationToken); } catch { /* already best-effort */ }
            throw;
        }
    }

    private static string GenerateSellerAlias() => $"Seller_{Guid.NewGuid().ToString("N")[..6]}";

    private static string GenerateAgencyCode(string agencyName)
    {
        var slug = new string(agencyName.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var prefix = slug.Length >= 4 ? slug[..4] : slug.PadRight(4, 'X');
        return $"{prefix}{Random.Shared.Next(100, 999)}";
    }
}
