using Thrivts.Domain.Common;

namespace Thrivts.Domain.Identity;

/// <summary>
/// Only the SHA-256 of the token is stored, never the token itself. Tokens rotate on every use and
/// share a FamilyId: if an already-used token is presented again (it was stolen and replayed), the
/// whole family is revoked and the user must sign in again.
/// </summary>
public class RefreshToken : Entity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public Guid FamilyId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedReason { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }

    private RefreshToken()
    {
    }

    public RefreshToken(Guid userId, string tokenHash, Guid familyId, DateTimeOffset createdAt, DateTimeOffset expiresAt, string? ip, string? userAgent)
    {
        UserId = userId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        CreatedByIp = ip;
        UserAgent = userAgent is { Length: > 300 } ? userAgent[..300] : userAgent;
    }

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now, string reason, Guid? replacedBy = null)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = now;
        RevokedReason = reason;
        ReplacedByTokenId = replacedBy;
    }
}
