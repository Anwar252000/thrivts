namespace Thrivts.Application.Common.Models;

/// <summary>Standard paged-list envelope for admin list endpoints — avoids ever returning an
/// unbounded row set from a table that can grow into the thousands (deals, requirements, etc.).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public static (int Page, int PageSize) Normalize(int page, int pageSize, int maxPageSize = 100)
    {
        var normalizedPage = page < 1 ? 1 : page;
        var normalizedPageSize = pageSize is < 1 or > 100 ? 25 : Math.Min(pageSize, maxPageSize);
        return (normalizedPage, normalizedPageSize);
    }
}
