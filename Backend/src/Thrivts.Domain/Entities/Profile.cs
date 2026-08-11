using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per Supabase auth user. Id == auth.uid() (the JWT `sub` claim) — never a separate
/// surrogate key, so a profile can always be looked up directly from the validated token.
/// Approval/active-status lifecycle (approve/reject/block/unblock) is shared across every role
/// (THRIVTS_FLOW_MATRIX.md §1.4) — not duplicated per role table.
/// </summary>
public class Profile : BaseEntity, IAggregateRoot
{
    public UserRole Role { get; private set; }
    public string Email { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? WhatsApp { get; private set; }
    public string FullName { get; private set; } = default!;
    public LanguagePref LanguagePref { get; private set; } = LanguagePref.En;
    public ApprovalStatus ApprovalStatus { get; private set; } = ApprovalStatus.Pending;
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? SignupIp { get; private set; }
    public string? SignupUserAgent { get; private set; }

    private Profile()
    {
        // EF Core
    }

    public Profile(Guid authUserId, UserRole role, string email, string fullName, string? signupIp = null, string? signupUserAgent = null)
    {
        Id = authUserId;
        Role = role;
        Email = email;
        FullName = fullName;
        SignupIp = signupIp;
        SignupUserAgent = signupUserAgent;
    }

    /// <summary>Replaces approve_seller (and the equivalent admin buyer-approval path).</summary>
    public void Approve(Guid approvedBy, DateTimeOffset occurredAt)
    {
        ApprovalStatus = ApprovalStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = occurredAt;
        RejectionReason = null;
    }

    /// <summary>Replaces reject_seller / rejectUser — a rejected profile is also deactivated
    /// (matches the live admin.html rejectUser(), which sets is_active: false alongside the
    /// rejected status; a rejected applicant should not be able to sign back in).</summary>
    public void Reject(string reason)
    {
        ApprovalStatus = ApprovalStatus.Rejected;
        RejectionReason = reason;
        IsActive = false;
    }

    /// <summary>Replaces block_seller / admin_block_buyer.</summary>
    public void Block()
    {
        ApprovalStatus = ApprovalStatus.Suspended;
        IsActive = false;
    }

    /// <summary>Replaces unblock_seller / admin_unblock_buyer.</summary>
    public void Unblock()
    {
        ApprovalStatus = ApprovalStatus.Approved;
        IsActive = true;
    }

    public void RecordLogin(DateTimeOffset occurredAt) => LastLoginAt = occurredAt;

    public void UpdateContactDetails(string? phone, string? whatsApp)
    {
        Phone = phone;
        WhatsApp = whatsApp;
    }

    public void SetLanguagePreference(LanguagePref languagePref) => LanguagePref = languagePref;
}
