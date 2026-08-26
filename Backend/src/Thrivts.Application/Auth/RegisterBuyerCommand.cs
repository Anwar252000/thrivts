using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Auth;

/// <summary>Replaces buyer.html's handleSignup() — self-service buyer application. Sends a
/// confirmation email via Supabase Auth's own signup flow and stashes every field below as
/// user_metadata; no Profile/Buyer row is written yet. CompleteBuyerRegistrationCommand does that,
/// once the applicant has clicked the confirmation link and proven the email is real.</summary>
public sealed record RegisterBuyerCommand(
    string Email, string Password, string FullName, string? Phone, string? WhatsApp, LanguagePref Language,
    string CompanyName, string? Website, string Country, string? City, string? Instagram,
    int? EstimatedMonthlyVolumePcs, string? TypicalRequirementType, int[]? CategoryIds,
    string? AgencyRef, string? ReferralCode) : ICommand<ErrorOr<Success>>;
