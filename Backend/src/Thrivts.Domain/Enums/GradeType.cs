namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors the live schema's grade_type enum. The DB defines 6 labels (A, A/B, B, Mixed, A_B,
/// MIXED) — A_B and MIXED are legacy-casing duplicates of A/B and Mixed from earlier data entry,
/// unused by any current row. This enum models the 4 real values and is natively mapped via
/// Npgsql's MapEnum (see NpgsqlEnumMapping's GradeTypeNameTranslator for the "A/B" label, which no
/// automatic casing rule can derive from "AB").
/// </summary>
public enum GradeType
{
    A,
    AB,
    B,
    Mixed
}
