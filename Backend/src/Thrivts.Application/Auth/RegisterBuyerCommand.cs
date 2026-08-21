using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Auth;

/// <summary>Replaces buyer.html's handleSignup() — self-service buyer application. Creates a
/// pending Profile+Buyer (never auto-approved; that's Admin's SetBuyerApprovalStatusCommand) and
/// signs the new user straight in so the frontend can show the pending screen immediately.</summary>
public sealed record RegisterBuyerCommand(
    string Email, string Password, string FullName, string? Phone, string? WhatsApp, LanguagePref Language,
    string CompanyName, string? Website, string Country, string? City, string? Instagram,
    int? EstimatedMonthlyVolumePcs, string? TypicalRequirementType, int[]? CategoryIds,
    string? AgencyRef, string? ReferralCode) : ICommand<ErrorOr<SupabaseSession>>;
