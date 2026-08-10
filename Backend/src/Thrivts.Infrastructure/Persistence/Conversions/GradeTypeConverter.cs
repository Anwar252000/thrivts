using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Thrivts.Domain.Enums;

namespace Thrivts.Infrastructure.Persistence.Conversions;

/// <summary>
/// grade_type is irregular (A, A/B, B, Mixed, plus legacy-casing duplicates A_B and MIXED) — see
/// GradeType. Always writes the canonical spelling; accepts either spelling on read.
/// </summary>
public class GradeTypeConverter : ValueConverter<GradeType, string>
{
    public GradeTypeConverter() : base(
        grade => grade == GradeType.A ? "A" : grade == GradeType.AB ? "A/B" : grade == GradeType.B ? "B" : "Mixed",
        value => value == "A" ? GradeType.A : value == "A/B" || value == "A_B" ? GradeType.AB : value == "B" ? GradeType.B : GradeType.Mixed)
    {
    }
}
