using System.Buffers.Text;
using System.Text;

namespace Thrivts.Application.Common.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize = 200)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize < 1 ? 50 : Math.Min(pageSize, maxPageSize);
        return (normalizedPage, normalizedPageSize);
    }
}

/// <summary>
/// Keyset ("seek") pagination for the long, ever-growing lists (gate history, activity, audit,
/// expenses). Unlike OFFSET paging its cost stays constant on page 1 or page 5,000, and new rows
/// arriving mid-scroll never shift or duplicate items. The cursor is an opaque token the client
/// passes back as-is.
/// </summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor)
{
    public const int DefaultSize = 50;
    public const int MaxSize = 200;

    public static int NormalizeSize(int? size) => size is null or < 1 ? DefaultSize : Math.Min(size.Value, MaxSize);
}

public readonly record struct Cursor(DateTimeOffset At, Guid Id)
{
    public string Encode() => Base64Url.EncodeToString(Encoding.UTF8.GetBytes($"{At.UtcTicks}:{Id:N}"));

    public static Cursor? Decode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
            return null;

        try
        {
            var parts = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token)).Split(':');
            return parts.Length == 2 && long.TryParse(parts[0], out var ticks) && Guid.TryParseExact(parts[1], "N", out var id)
                ? new Cursor(new DateTimeOffset(ticks, TimeSpan.Zero), id)
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
