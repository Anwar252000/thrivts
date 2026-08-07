using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A dispute raised against a Deal (drives DealStatus.Disputed). NOTE: inferred — the disputes
/// table was not in the exported SQL set; verify the exact column set once the schema export lands.
/// </summary>
public class Dispute : BaseEntity, IAggregateRoot
{
    public Guid DealId { get; private set; }
    public Guid RaisedByProfileId { get; private set; }
    public string Reason { get; private set; } = default!;
    public DisputeStatus Status { get; private set; } = DisputeStatus.Open;
    public string? ResolutionNotes { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    private Dispute()
    {
        // EF Core
    }

    public Dispute(Guid dealId, Guid raisedByProfileId, string reason)
    {
        DealId = dealId;
        RaisedByProfileId = raisedByProfileId;
        Reason = reason;
    }

    public void BeginReview() => Status = DisputeStatus.UnderReview;

    public void Resolve(string resolutionNotes)
    {
        Status = DisputeStatus.Resolved;
        ResolutionNotes = resolutionNotes;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Reject(string resolutionNotes)
    {
        Status = DisputeStatus.Rejected;
        ResolutionNotes = resolutionNotes;
        ResolvedAt = DateTimeOffset.UtcNow;
    }
}
