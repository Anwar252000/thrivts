using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>A dispute raised against a Deal (drives DealStatus.Disputed).</summary>
public class Dispute : BaseEntity, IAggregateRoot
{
    public string DisputeNumber { get; private set; } = default!;
    public Guid DealId { get; private set; }
    public Guid RaisedBy { get; private set; }
    public string? Category { get; private set; }
    public string Description { get; private set; } = default!;
    public string? RequestedResolution { get; private set; }
    public string? AttachmentsJson { get; private set; }

    public DisputeStatus Status { get; private set; } = DisputeStatus.Open;
    public string? Resolution { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public decimal RefundAmountUsd { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }

    private Dispute()
    {
        // EF Core
    }

    public Dispute(string disputeNumber, Guid dealId, Guid raisedBy, string description,
        string? category = null, string? requestedResolution = null)
    {
        DisputeNumber = disputeNumber;
        DealId = dealId;
        RaisedBy = raisedBy;
        Description = description;
        Category = category;
        RequestedResolution = requestedResolution;
    }

    public void BeginInvestigation() => Status = DisputeStatus.Investigating;

    public void Resolve(string resolution, string? resolutionNotes, decimal refundAmountUsd, Guid resolvedBy, DateTimeOffset occurredAt)
    {
        Status = DisputeStatus.Resolved;
        Resolution = resolution;
        ResolutionNotes = resolutionNotes;
        RefundAmountUsd = refundAmountUsd;
        ResolvedBy = resolvedBy;
        ResolvedAt = occurredAt;
    }

    public void Reject(string? resolutionNotes, Guid resolvedBy, DateTimeOffset occurredAt)
    {
        Status = DisputeStatus.Rejected;
        ResolutionNotes = resolutionNotes;
        ResolvedBy = resolvedBy;
        ResolvedAt = occurredAt;
    }

    public void Withdraw() => Status = DisputeStatus.Withdrawn;
}
