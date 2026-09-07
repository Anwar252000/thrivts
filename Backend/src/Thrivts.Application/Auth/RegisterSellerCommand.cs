using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Auth;

/// <summary>Replaces seller.html's handleSignup() — self-service seller application. Sends a
/// confirmation email via Supabase Auth's own signup flow and stashes every field below as
/// user_metadata; no Profile/Seller row is written yet. CompleteSellerRegistrationCommand does
/// that, once the applicant has clicked the confirmation link and proven the email is real.</summary>
public sealed record RegisterSellerCommand(
    string Email, string Password, string FullName, string CompanyName, string Country, string? City,
    string? Phone, string? WhatsApp, int? YearsInBusiness, int? MonthlyVolumeCapacityPcs,
    string[]? CategoriesSupplied, LanguagePref Language) : ICommand<ErrorOr<Success>>;
