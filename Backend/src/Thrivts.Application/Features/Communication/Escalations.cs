using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Communication;

namespace Thrivts.Application.Features.Communication;

public sealed record EscalationDto(
    Guid Id, string FromName, EscalationSource Source, string Message, string? Department, DateOnly Date,
    string? Reply, DateTimeOffset? RepliedAt, bool HasUnreadReply, bool IsResolved, DateTimeOffset CreatedAt);

internal static class EscalationProjection
{
    public static readonly System.Linq.Expressions.Expression<Func<Escalation, EscalationDto>> ToDto = e => new EscalationDto(
        e.Id, e.FromName, e.Source, e.Message, e.Department, e.BusinessDate, e.Reply, e.RepliedAt,
        e.Reply != null && e.ReplyReadAt == null, e.IsResolved, e.CreatedAt);

    public static readonly Func<Escalation, EscalationDto> Map = ToDto.Compile();
}

public sealed record RaiseEscalationCommand(string Message, string? Department) : ICommand<EscalationDto>;

public sealed class RaiseEscalationValidator : AbstractValidator<RaiseEscalationCommand>
{
    public RaiseEscalationValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Department).MaximumLength(40);
    }
}

public sealed class RaiseEscalationHandler(IAppDbContext db, ICurrentUser user, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<RaiseEscalationCommand, EscalationDto>
{
    public async ValueTask<EscalationDto> Handle(RaiseEscalationCommand command, CancellationToken ct)
    {
        var escalation = new Escalation(user.RequireUserId(), user.Name ?? "Staff", EscalationSource.Message, command.Message, command.Department, clock.Today);
        db.Escalations.Add(escalation);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Escalations, RealtimeAudience.Owner, null, ct);
        return EscalationProjection.Map(escalation);
    }
}

/// <summary>The sender's own escalations — unread owner replies first.</summary>
public sealed record GetMyEscalationsQuery : IQuery<IReadOnlyList<EscalationDto>>;

public sealed class GetMyEscalationsHandler(IAppDbContext db, ICurrentUser user) : IQueryHandler<GetMyEscalationsQuery, IReadOnlyList<EscalationDto>>
{
    public async ValueTask<IReadOnlyList<EscalationDto>> Handle(GetMyEscalationsQuery query, CancellationToken ct)
    {
        var userId = user.RequireUserId();
        return await db.Escalations.AsNoTracking()
            .Where(e => e.FromUserId == userId)
            .OrderByDescending(e => e.Reply != null && e.ReplyReadAt == null).ThenByDescending(e => e.CreatedAt)
            .Take(100)
            .Select(EscalationProjection.ToDto)
            .ToListAsync(ct);
    }
}

public sealed record MarkEscalationRepliesReadCommand : ICommand<Success>;

public sealed class MarkEscalationRepliesReadHandler(IAppDbContext db, ICurrentUser user, IClock clock) : ICommandHandler<MarkEscalationRepliesReadCommand, Success>
{
    public async ValueTask<Success> Handle(MarkEscalationRepliesReadCommand command, CancellationToken ct)
    {
        var userId = user.RequireUserId();
        var now = clock.UtcNow;
        await db.Escalations
            .Where(e => e.FromUserId == userId && e.Reply != null && e.ReplyReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.ReplyReadAt, now), ct);
        return Result.Success;
    }
}

// ---------- Owner side ----------

public sealed record GetEscalationsQuery(bool OpenOnly, string? Cursor, int? Size) : IQuery<CursorPage<EscalationDto>>;

public sealed class GetEscalationsHandler(IAppDbContext db) : IQueryHandler<GetEscalationsQuery, CursorPage<EscalationDto>>
{
    public async ValueTask<CursorPage<EscalationDto>> Handle(GetEscalationsQuery query, CancellationToken ct)
    {
        var size = CursorPage<EscalationDto>.NormalizeSize(query.Size);
        var escalations = db.Escalations.AsNoTracking();
        if (query.OpenOnly)
            escalations = escalations.Where(e => !e.IsResolved);
        if (Cursor.Decode(query.Cursor) is { } c)
            escalations = escalations.Where(e => e.CreatedAt < c.At || (e.CreatedAt == c.At && e.Id.CompareTo(c.Id) < 0));

        var page = await escalations.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id).Take(size).Select(EscalationProjection.ToDto).ToListAsync(ct);
        var next = page.Count == size ? new Cursor(page[^1].CreatedAt, page[^1].Id).Encode() : null;
        return new CursorPage<EscalationDto>(page, next);
    }
}

public sealed record ReplyEscalationCommand(Guid Id, string Reply, bool Resolve) : ICommand<ErrorOr<Success>>;

public sealed class ReplyEscalationValidator : AbstractValidator<ReplyEscalationCommand>
{
    public ReplyEscalationValidator() => RuleFor(x => x.Reply).NotEmpty().MaximumLength(2000);
}

public sealed class ReplyEscalationHandler(IAppDbContext db, ICurrentUser user, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<ReplyEscalationCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(ReplyEscalationCommand command, CancellationToken ct)
    {
        var escalation = await db.Escalations.FirstOrDefaultAsync(e => e.Id == command.Id, ct);
        if (escalation is null)
            return Error.NotFound(description: "Escalation not found.");

        escalation.ReplyWith(command.Reply, user.RequireUserId(), clock.UtcNow);
        if (command.Resolve)
            escalation.Resolve(clock.UtcNow);

        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Escalations, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public sealed record SetEscalationResolvedCommand(Guid Id, bool Resolved) : ICommand<ErrorOr<Success>>;

public sealed class SetEscalationResolvedHandler(IAppDbContext db, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<SetEscalationResolvedCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetEscalationResolvedCommand command, CancellationToken ct)
    {
        var escalation = await db.Escalations.FirstOrDefaultAsync(e => e.Id == command.Id, ct);
        if (escalation is null)
            return Error.NotFound(description: "Escalation not found.");

        if (command.Resolved)
            escalation.Resolve(clock.UtcNow);
        else
            escalation.Reopen();

        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Escalations, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

// ---------- Activity feed ----------

public sealed record ActivityDto(Guid Id, ActivityArea Area, string Type, string Text, string? UserName, DateTimeOffset OccurredAt);

public sealed record GetActivityQuery(ActivityArea? Area, string? Search, DateOnly? From, DateOnly? To, string? Cursor, int? Size) : IQuery<CursorPage<ActivityDto>>;

public sealed class GetActivityHandler(IAppDbContext db) : IQueryHandler<GetActivityQuery, CursorPage<ActivityDto>>
{
    private static readonly TimeSpan Pakistan = TimeSpan.FromHours(5);

    public async ValueTask<CursorPage<ActivityDto>> Handle(GetActivityQuery query, CancellationToken ct)
    {
        var size = CursorPage<ActivityDto>.NormalizeSize(query.Size);
        var events = db.ActivityEvents.AsNoTracking();
        if (query.Area is { } area)
            events = events.Where(e => e.Area == area);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            events = events.Where(e => e.Text.ToLower().Contains(term));
        }

        if (query.From is { } from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), Pakistan).ToUniversalTime();
            events = events.Where(e => e.OccurredAt >= start);
        }

        if (query.To is { } to)
        {
            var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), Pakistan).ToUniversalTime();
            events = events.Where(e => e.OccurredAt < end);
        }

        if (Cursor.Decode(query.Cursor) is { } c)
            events = events.Where(e => e.OccurredAt < c.At || (e.OccurredAt == c.At && e.Id.CompareTo(c.Id) < 0));

        var page = await events.OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id).Take(size)
            .Select(e => new ActivityDto(e.Id, e.Area, e.Type, e.Text, e.UserName, e.OccurredAt))
            .ToListAsync(ct);
        var next = page.Count == size ? new Cursor(page[^1].OccurredAt, page[^1].Id).Encode() : null;
        return new CursorPage<ActivityDto>(page, next);
    }
}

/// <summary>The whole activity log of one area as a stream, newest first — for CSV export. The feed
/// grows forever, so rows go straight from the database cursor to the response.</summary>
public sealed record ExportActivityQuery(ActivityArea Area) : IStreamQuery<ActivityDto>;

public sealed class ExportActivityHandler(IAppDbContext db) : IStreamQueryHandler<ExportActivityQuery, ActivityDto>
{
    public IAsyncEnumerable<ActivityDto> Handle(ExportActivityQuery query, CancellationToken ct) =>
        db.ActivityEvents.AsNoTracking()
            .Where(e => e.Area == query.Area)
            .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
            .Select(e => new ActivityDto(e.Id, e.Area, e.Type, e.Text, e.UserName, e.OccurredAt))
            .AsAsyncEnumerable();
}
