namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors the live schema's grade_type enum. The DB defines 6 labels (A, A/B, B, Mixed, A_B,
/// MIXED) — A_B and MIXED are legacy-casing duplicates of A/B and Mixed from earlier data entry.
/// This enum models the 4 real values; GradeTypeConverter (Infrastructure) maps both legacy
/// spellings onto AB/Mixed on read, and always writes the canonical "A/B" / "Mixed" spelling.
/// </summary>
public enum GradeType
{
    A,
    AB,
    B,
    Mixed
}
