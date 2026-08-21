using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Auth;

public sealed class RegisterBuyerCommandHandler : ICommandHandler<RegisterBuyerCommand, ErrorOr<SupabaseSession>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly ISupabaseAdminClient _supabaseAdmin;
    private readonly ISupabaseAuthClient _supabaseAuth;

    public RegisterBuyerCommandHandler(IApplicationDbContext db, IDateTimeProvider clock, ISupabaseAdminClient supabaseAdmin, ISupabaseAuthClient supabaseAuth)
    {
        _db = db;
        _clock = clock;
        _supabaseAdmin = supabaseAdmin;
        _supabaseAuth = supabaseAuth;
    }

    public async ValueTask<ErrorOr<SupabaseSession>> Handle(RegisterBuyerCommand command, CancellationToken cancellationToken)
    {
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

            var profile = new Profile(authUserId, UserRole.Buyer, command.Email, command.FullName);
            profile.UpdateContactDetails(command.Phone, command.WhatsApp);
            profile.SetLanguagePreference(command.Language);
            _db.Profiles.Add(profile);

            var buyer = new Buyer(authUserId, command.CompanyName, command.Country);

            var categoryNames = command.CategoryIds is { Length: > 0 }
                ? await _db.Categories.Where(c => command.CategoryIds.Contains(c.Id)).Select(c => c.Name).ToArrayAsync(cancellationToken)
                : null;
            buyer.CompleteSignupProfile(command.City, command.Website, command.Instagram,
                command.EstimatedMonthlyVolumePcs, command.TypicalRequirementType, categoryNames);

            if (!string.IsNullOrWhiteSpace(command.AgencyRef))
            {
                var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.AgencyCode.ToLower() == command.AgencyRef.ToLower(), cancellationToken);
                if (agency is not null)
                {
                    buyer.AttributeToAgency(agency.Id, command.AgencyRef, now);
                    agency.RecordReferredBuyer();
                }
            }

            if (!string.IsNullOrWhiteSpace(command.ReferralCode))
            {
                var influencer = await _db.Influencers.FirstOrDefaultAsync(i => i.ReferralCode.ToLower() == command.ReferralCode.ToLower(), cancellationToken);
                if (influencer is not null)
                    buyer.LinkReferral(influencer.Id, command.ReferralCode, now);
            }

            _db.Buyers.Add(buyer);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Best-effort compensation, same pattern as Admin.Users.CreateUserCommand — an orphaned
            // auth identity with no Profile row can never sign in, but would block re-registering
            // the same email later if left behind.
            try { await _supabaseAdmin.DeleteUserAsync(authUserId, cancellationToken); } catch { /* already best-effort */ }
            throw;
        }

        try
        {
            return await _supabaseAuth.SignInWithPasswordAsync(command.Email, command.Password, cancellationToken);
        }
        catch (SupabaseAuthException ex)
        {
            // The application itself succeeded — only the auto-login convenience failed. Do not
            // roll back the registration; the caller can sign in manually.
            return Error.Failure(description: $"Application submitted, but automatic sign-in failed ({ex.Message}). Please sign in manually.");
        }
    }
}
