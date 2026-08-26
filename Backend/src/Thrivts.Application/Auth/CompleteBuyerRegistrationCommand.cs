using System.Text.Json;
using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Auth;

/// <summary>
/// Runs once the applicant has clicked the confirmation link from RegisterBuyerCommand's email —
/// the frontend calls this bearing the access_token GoTrue put in that link's redirect (already a
/// fully valid session token; the caller doesn't need the Buyer role claim yet since the Profile
/// row is what this handler is about to create). Reads the signup form's fields back out of
/// Supabase's raw_user_meta_data (nothing was written to our DB before this point) and creates the
/// Profile + Buyer, still Pending — Admin's SetBuyerApprovalStatusCommand takes it from there.
/// Idempotent: a repeat call (double-click, retry after a transient failure) is a no-op success.
/// </summary>
public sealed record CompleteBuyerRegistrationCommand : ICommand<ErrorOr<Success>>;

public sealed class CompleteBuyerRegistrationCommandHandler : ICommandHandler<CompleteBuyerRegistrationCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISupabaseAdminClient _supabaseAdmin;

    public CompleteBuyerRegistrationCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, ISupabaseAdminClient supabaseAdmin)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _supabaseAdmin = supabaseAdmin;
    }

    public async ValueTask<ErrorOr<Success>> Handle(CompleteBuyerRegistrationCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var userId = _currentUser.UserId.Value;

        if (await _db.Profiles.AnyAsync(p => p.Id == userId, cancellationToken))
            return Result.Success;

        SupabaseAdminUser authUser;
        try
        {
            authUser = await _supabaseAdmin.GetUserAsync(userId, cancellationToken);
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Failure(description: ex.Message);
        }

        if (!authUser.EmailConfirmed)
            return Error.Validation(description: "Please confirm your email before continuing.");

        var meta = authUser.Metadata;
        var fullName = GetString(meta, "full_name") ?? authUser.Email;
        var phone = GetString(meta, "phone");
        var whatsApp = GetString(meta, "whatsapp");
        var languagePref = Enum.TryParse<LanguagePref>(GetString(meta, "language_pref"), ignoreCase: true, out var lang) ? lang : LanguagePref.En;
        var companyName = GetString(meta, "company_name") ?? fullName;
        var website = GetString(meta, "website");
        var country = GetString(meta, "country") ?? "Unknown";
        var city = GetString(meta, "city");
        var instagram = GetString(meta, "instagram");
        var volume = GetInt(meta, "estimated_monthly_volume_pcs");
        var reqType = GetString(meta, "typical_requirement_type");
        var categoryIds = GetIntArray(meta, "category_ids");
        var agencyRef = GetString(meta, "agency_ref");
        var referralCode = GetString(meta, "referral_code");

        var now = _clock.UtcNow;

        var profile = new Profile(userId, UserRole.Buyer, authUser.Email, fullName);
        profile.UpdateContactDetails(phone, whatsApp);
        profile.SetLanguagePreference(languagePref);
        _db.Profiles.Add(profile);

        var buyer = new Buyer(userId, companyName, country);
        var categoryNames = categoryIds is { Length: > 0 }
            ? await _db.Categories.Where(c => categoryIds.Contains(c.Id)).Select(c => c.Name).ToArrayAsync(cancellationToken)
            : null;
        buyer.CompleteSignupProfile(city, website, instagram, volume, reqType, categoryNames);

        if (!string.IsNullOrWhiteSpace(agencyRef))
        {
            var agency = await _db.Agencies.FirstOrDefaultAsync(a => a.AgencyCode.ToLower() == agencyRef.ToLower(), cancellationToken);
            if (agency is not null)
            {
                buyer.AttributeToAgency(agency.Id, agencyRef, now);
                agency.RecordReferredBuyer();
            }
        }

        if (!string.IsNullOrWhiteSpace(referralCode))
        {
            var influencer = await _db.Influencers.FirstOrDefaultAsync(i => i.ReferralCode.ToLower() == referralCode.ToLower(), cancellationToken);
            if (influencer is not null)
                buyer.LinkReferral(influencer.Id, referralCode, now);
        }

        _db.Buyers.Add(buyer);
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private static string? GetString(JsonElement meta, string key) =>
        meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? GetInt(JsonElement meta, string key) =>
        meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : null;

    private static int[]? GetIntArray(JsonElement meta, string key)
    {
        if (meta.ValueKind != JsonValueKind.Object || !meta.TryGetProperty(key, out var v) || v.ValueKind != JsonValueKind.Array)
            return null;

        return v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Number).Select(e => e.GetInt32()).ToArray();
    }
}
