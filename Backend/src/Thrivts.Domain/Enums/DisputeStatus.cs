namespace Thrivts.Domain.Enums;

/// <summary>
/// NOTE: inferred — the live schema's disputes table was not in the exported SQL set.
/// Verify these values (and any missing ones) once the schema export lands.
/// </summary>
public enum DisputeStatus
{
    Open,
    UnderReview,
    Resolved,
    Rejected
}
