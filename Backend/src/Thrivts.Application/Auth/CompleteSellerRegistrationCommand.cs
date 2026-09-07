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
/// Runs once the applicant has clicked the confirmation link from RegisterSellerCommand's email —
/// mirrors CompleteBuyerRegistrationCommand exactly (see its doc comment). Reads the signup form's
/// fields back out of Supabase's raw_user_meta_data and creates the Profile + Seller, still
/// Pending — Admin's seller-approval command takes it from there. Idempotent.
/// </summary>
public sealed record CompleteSellerRegistrationCommand : ICommand<ErrorOr<Success>>;

public sealed class CompleteSellerRegistrationCommandHandler : ICommandHandler<CompleteSellerRegistrationCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly ISupabaseAdminClient _supabaseAdmin;

    public CompleteSellerRegistrationCommandHandler(
        IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, ISupabaseAdminClient supabaseAdmin)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _supabaseAdmin = supabaseAdmin;
    }

    public async ValueTask<ErrorOr<Success>> Handle(CompleteSellerRegistrationCommand command, CancellationToken cancellationToken)
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
        var companyName = GetString(meta, "company_name") ?? fullName;
        var country = GetString(meta, "country") ?? "Pakistan";
        var city = GetString(meta, "city") ?? "Unknown";
        var phone = GetString(meta, "phone");
        var whatsApp = GetString(meta, "whatsapp");
        var yearsInBusiness = GetInt(meta, "years_in_business");
        var monthlyVolumeCapacityPcs = GetInt(meta, "monthly_volume_capacity_pcs");
        var categoriesSupplied = GetStringArray(meta, "categories_supplied") ?? [];
        var languagePref = Enum.TryParse<LanguagePref>(GetString(meta, "language_pref"), ignoreCase: true, out var lang) ? lang : LanguagePref.En;

        var profile = new Profile(userId, UserRole.Seller, authUser.Email, fullName);
        profile.UpdateContactDetails(phone, whatsApp);
        profile.SetLanguagePreference(languagePref);
        _db.Profiles.Add(profile);

        var seller = new Seller(userId, GenerateSellerAlias(), city, categoriesSupplied, country);
        seller.CompleteSignupProfile(companyName, phone, whatsApp, yearsInBusiness, monthlyVolumeCapacityPcs, socialMediaJson: null);
        _db.Sellers.Add(seller);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private static string GenerateSellerAlias() => $"Seller_{Guid.NewGuid().ToString("N")[..6]}";

    private static string? GetString(JsonElement meta, string key) =>
        meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? GetInt(JsonElement meta, string key) =>
        meta.ValueKind == JsonValueKind.Object && meta.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var n) ? n : null;

    private static string[]? GetStringArray(JsonElement meta, string key)
    {
        if (meta.ValueKind != JsonValueKind.Object || !meta.TryGetProperty(key, out var v) || v.ValueKind != JsonValueKind.Array)
            return null;

        return v.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.String).Select(e => e.GetString()!).ToArray();
    }
}
