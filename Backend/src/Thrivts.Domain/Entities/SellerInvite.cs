using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>An admin-issued invite token a prospective seller redeems to sign up (THRIVTS_FLOW_MATRIX.md §1.2).</summary>
public class SellerInvite : BaseEntity, IAggregateRoot
{
    public string Token { get; private set; } = default!;
    public Guid? InvitedBy { get; private set; }
    public string CompanyName { get; private set; } = default!;
    public string? Phone { get; private set; }
    public string? Country { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public Guid? UsedBy { get; private set; }

    private SellerInvite()
    {
        // EF Core
    }

    public SellerInvite(string token, string companyName, DateTimeOffset expiresAt, Guid? invitedBy = null,
        string? phone = null, string? country = null, string? notes = null)
    {
        Token = token;
        CompanyName = companyName;
        ExpiresAt = expiresAt;
        InvitedBy = invitedBy;
        Phone = phone;
        Country = country;
        Notes = notes;
    }

    public void Redeem(Guid usedBy, DateTimeOffset occurredAt)
    {
        if (UsedAt is not null)
            throw new DomainException("This invite has already been used.");
        if (occurredAt > ExpiresAt)
            throw new DomainException("This invite has expired.");

        UsedAt = occurredAt;
        UsedBy = usedBy;
    }
}
