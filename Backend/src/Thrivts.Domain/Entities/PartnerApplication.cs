using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>A prospective influencer/partner's application (the live schema's partner_applications
/// table — replaces submit_partner_application from the migration plan's Application-layer table).</summary>
public class PartnerApplication : IAggregateRoot
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string? Instagram { get; private set; }
    public string? Tiktok { get; private set; }
    public string? Experience { get; private set; }
    public string? Platform { get; private set; }
    public string? ProfileLink { get; private set; }
    public string? About { get; private set; }
    public string Status { get; private set; } = "pending";
    public Guid? InfluencerId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; private set; }

    private PartnerApplication()
    {
        // EF Core
    }

    public PartnerApplication(string fullName, string email, string? instagram = null, string? tiktok = null,
        string? experience = null, string? platform = null, string? profileLink = null, string? about = null)
    {
        FullName = fullName;
        Email = email;
        Instagram = instagram;
        Tiktok = tiktok;
        Experience = experience;
        Platform = platform;
        ProfileLink = profileLink;
        About = about;
    }

    public void Approve(Guid influencerId, DateTimeOffset occurredAt)
    {
        Status = "approved";
        InfluencerId = influencerId;
        ReviewedAt = occurredAt;
    }

    public void Reject(DateTimeOffset occurredAt)
    {
        Status = "rejected";
        ReviewedAt = occurredAt;
    }
}
