using System.Text.RegularExpressions;

namespace Thrivts.Domain.Common;

public static partial class Text
{
    /// <summary>Case- and whitespace-insensitive key used for uniqueness (suppliers, items, order
    /// lines). The legacy app deduplicated by lower(trim(name)); this keeps that rule but enforces
    /// it with a unique index.</summary>
    public static string Normalize(string value) => MultiSpace().Replace(value.Trim(), " ").ToLowerInvariant();

    public static string Clean(string value) => MultiSpace().Replace(value.Trim(), " ");

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpace();
}
