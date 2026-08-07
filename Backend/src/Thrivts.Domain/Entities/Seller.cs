using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per seller. Id == the seller's auth user id (same convention as Profile).
/// Approval/active-status lifecycle (approve/reject/block/unblock) lives on Profile, not here —
/// THRIVTS_FLOW_MATRIX.md §1.4 confirms approval_status/is_active are profiles columns shared
/// across every role, not duplicated per role table.
/// PublicAlias is the ONLY identifier a buyer may ever see (the anonymity "moat" — see
/// README_HANDOVER.md). CompanyName/Phone/WhatsApp/Email/ReferenceContact must never be
/// serialized into a buyer-facing DTO; only AdminController-authorized responses may include them.
/// </summary>
public class Seller : BaseEntity, IAggregateRoot
{
    public string PublicAlias { get; private set; } = default!;
    public string CompanyName { get; private set; } = default!;
    public string? Tier { get; private set; }
    public bool KycVerified { get; private set; }

    public string? Phone { get; private set; }
    public string? WhatsApp { get; private set; }
    public string? Email { get; private set; }
    public string? ReferenceContact { get; private set; }

    /// <summary>
    /// Admin-managed, comma-delimited tag list controlling which requirements a seller can see
    /// (current_seller_tags() in the live schema). NOTE: the live UI stores this as a raw
    /// comma-separated string, which corrupts tags containing commas — a known fast-follow
    /// (see README_HANDOVER.md §4). Model as a proper collection once that's fixed.
    /// </summary>
    public string? Tags { get; private set; }

    private Seller()
    {
        // EF Core
    }

    public Seller(Guid authUserId, string publicAlias, string companyName)
    {
        Id = authUserId;
        PublicAlias = publicAlias;
        CompanyName = companyName;
    }

    public void VerifyKyc() => KycVerified = true;

    public void UnverifyKyc() => KycVerified = false;

    public void SetTags(string? tags) => Tags = tags;

    public void UpdateContactDetails(string? phone, string? whatsApp, string? email, string? referenceContact)
    {
        Phone = phone;
        WhatsApp = whatsApp;
        Email = email;
        ReferenceContact = referenceContact;
    }
}
