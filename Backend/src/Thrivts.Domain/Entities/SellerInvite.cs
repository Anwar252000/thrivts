using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

/// <summary>
/// An admin-issued invite token a prospective seller redeems to sign up (the live schema's
/// seller_invites table — confirmed by THRIVTS_FLOW_MATRIX.md §1.2, exact column set not in the
/// exported SQL set beyond token/used_at).
/// </summary>
public class SellerInvite : BaseEntity, IAggregateRoot
{
    public string Token { get; private set; } = default!;
    public string? Email { get; private set; }
    public Guid InvitedByProfileId { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private SellerInvite()
    {
        // EF Core
    }

    public SellerInvite(string token, Guid invitedByProfileId, string? email = null)
    {
        Token = token;
        InvitedByProfileId = invitedByProfileId;
        Email = email;
    }

    public void MarkUsed()
    {
        if (UsedAt is not null)
            throw new DomainException("This invite has already been used.");

        UsedAt = DateTimeOffset.UtcNow;
    }
}
