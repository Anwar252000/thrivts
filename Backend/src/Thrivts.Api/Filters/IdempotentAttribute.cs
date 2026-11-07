using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Infrastructure.Persistence;

namespace Thrivts.Api.Filters;

/// <summary>
/// Opt-in exactly-once semantics for writes that must never double up (gate arrivals, payments,
/// dispatches). When the request carries an Idempotency-Key header:
///   * first time: a placeholder row claims the key, the action runs, and a 2xx response is stored;
///   * a retry with the same key and same body replays the stored response;
///   * a retry while the first attempt is still running gets 409;
///   * the same key with a different body gets 422 — that is a client bug, not a retry.
/// Failed attempts release the key so the client can genuinely retry. Keys live 24 hours.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "Idempotency-Key";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var key = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrEmpty(key))
        {
            await next();
            return;
        }

        if (key.Length is < 8 or > 100)
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails { Title = "Idempotency-Key must be 8–100 characters.", Status = 400 });
            return;
        }

        var services = context.HttpContext.RequestServices;
        var db = services.GetRequiredService<ThrivtsDbContext>();
        var clock = services.GetRequiredService<IClock>();
        var userId = services.GetRequiredService<ICurrentUser>().UserId ?? Guid.Empty;
        var scopedKey = $"{userId:N}:{key}";
        var requestHash = Hash(context);

        var existing = await db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Key == scopedKey);
        if (existing is not null && existing.ExpiresAt > clock.UtcNow)
        {
            context.Result = Replay(existing, requestHash);
            return;
        }

        if (existing is not null)
            await db.IdempotencyRecords.Where(r => r.Key == scopedKey).ExecuteDeleteAsync();

        try
        {
            var now = clock.UtcNow;
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO idempotency_records (key, user_id, request_hash, status_code, created_at, expires_at)
                VALUES ({scopedKey}, {userId}, {requestHash}, 0, {now}, {now + Lifetime})
                """);
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            context.Result = Conflict("This request is already being processed.");
            return;
        }

        var executed = await next();

        if (executed.Exception is null && executed.Result is ObjectResult { StatusCode: >= 200 and < 300 or null } result)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(result.Value, result.DeclaredType ?? result.Value?.GetType() ?? typeof(object),
                services.GetRequiredService<Microsoft.Extensions.Options.IOptions<JsonOptions>>().Value.JsonSerializerOptions);
            var status = result.StatusCode ?? 200;
            await db.IdempotencyRecords.Where(r => r.Key == scopedKey).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.StatusCode, status)
                .SetProperty(r => r.ContentType, "application/json")
                .SetProperty(r => r.ResponseBody, body));
        }
        else if (executed.Exception is null && executed.Result is StatusCodeResult { StatusCode: >= 200 and < 300 } statusOnly)
        {
            await db.IdempotencyRecords.Where(r => r.Key == scopedKey)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.StatusCode, statusOnly.StatusCode));
        }
        else
        {
            await db.IdempotencyRecords.Where(r => r.Key == scopedKey).ExecuteDeleteAsync();
        }
    }

    private static IActionResult Replay(IdempotencyRecord record, string requestHash)
    {
        if (record.RequestHash != requestHash)
            return new UnprocessableEntityObjectResult(new ProblemDetails { Title = "This Idempotency-Key was already used for a different request.", Status = 422 });
        if (record.StatusCode == 0)
            return Conflict("This request is already being processed.");
        if (record.ResponseBody is null)
            return new StatusCodeResult(record.StatusCode);

        return new ContentResult
        {
            StatusCode = record.StatusCode,
            Content = Encoding.UTF8.GetString(record.ResponseBody),
            ContentType = record.ContentType ?? "application/json",
        };
    }

    private static ObjectResult Conflict(string title) =>
        new(new ProblemDetails { Title = title, Status = 409 }) { StatusCode = 409 };

    private static string Hash(ActionExecutingContext context)
    {
        var payload = JsonSerializer.Serialize(context.ActionArguments
            .Where(a => a.Value is not CancellationToken)
            .OrderBy(a => a.Key)
            .Select(a => new { a.Key, a.Value }));
        var raw = $"{context.HttpContext.Request.Method}|{context.HttpContext.Request.Path}|{payload}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }
}
