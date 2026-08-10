namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's dispute_status enum exactly (disputes.status is plain text
/// in the live DB, but this enum type exists and matches the values actually used).</summary>
public enum DisputeStatus
{
    Open,
    Investigating,
    Resolved,
    Rejected,
    Withdrawn
}
