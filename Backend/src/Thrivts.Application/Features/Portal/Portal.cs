using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Orders;

namespace Thrivts.Application.Features.Portal;

/// <summary>
/// The buyers' order portals (legacy: one static site per buyer reading the database directly).
/// A buyer proves who they are with the order's login code — there is no account. What they see is
/// deliberately narrow: their one order's progress, using the owner's client-facing "ready" figure.
/// Never the order value, payments, internal ready counts or anything about other orders.
/// </summary>
public static class PortalRules
{
    public const int MinCodeLength = 6;
    public const int MaxCodeLength = 40;

    /// <summary>The same message for "no such code" and "malformed code", so the endpoint can't be
    /// used to learn which codes exist.</summary>
    public static readonly Error UnknownCode = Error.NotFound("Portal.UnknownCode", "That code wasn't recognised. Check it and try again.");

    public static bool LooksValid(string? code) =>
        code is { Length: >= MinCodeLength and <= MaxCodeLength } && code.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}

public sealed record PortalLineDto(string ItemName, string? Grade, string? Category, int Required, int Ready, OrderLineStatus Status);

public sealed record PortalMessageDto(ClientMessageType Type, string Message, string? RequestedWhen, bool IsDone, DateTimeOffset CreatedAt);

public sealed record PortalOrderDto(
    string OrderName, string? ClientName, ClientOrderStatus Status, DateOnly? Deadline, bool IsCompleted, DateOnly? DispatchDate,
    int Required, int Ready, IReadOnlyList<PortalLineDto> Lines, IReadOnlyList<PortalMessageDto> Messages, DateTimeOffset UpdatedAt);

public sealed record GetPortalOrderQuery(string Code) : IQuery<ErrorOr<PortalOrderDto>>;

public sealed class GetPortalOrderHandler(IAppDbContext db) : IQueryHandler<GetPortalOrderQuery, ErrorOr<PortalOrderDto>>
{
    public async ValueTask<ErrorOr<PortalOrderDto>> Handle(GetPortalOrderQuery query, CancellationToken ct)
    {
        if (!PortalRules.LooksValid(query.Code))
            return PortalRules.UnknownCode;

        var code = query.Code.Trim();
        var order = await db.ClientOrders.AsNoTracking()
            .Where(o => o.LoginCode == code)
            .Select(o => new
            {
                o.Id, o.Name, o.ClientName, o.Status, o.Deadline, o.IsCompleted, o.DispatchDate,
                UpdatedAt = o.UpdatedAt ?? o.CreatedAt,
                Lines = o.Lines.OrderBy(l => l.Seq)
                    .Select(l => new PortalLineDto(l.ItemName, l.Grade, l.Category, l.Required, l.ClientReady < l.Required ? l.ClientReady : l.Required, l.Status))
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);
        if (order is null)
            return PortalRules.UnknownCode;

        var messages = await db.ClientMessages.AsNoTracking()
            .Where(m => m.ClientOrderId == order.Id)
            .OrderByDescending(m => m.CreatedAt).Take(20)
            .Select(m => new PortalMessageDto(m.Type, m.Message, m.RequestedWhen, m.IsDone, m.CreatedAt))
            .ToListAsync(ct);

        return new PortalOrderDto(order.Name, order.ClientName, order.Status, order.Deadline, order.IsCompleted, order.DispatchDate,
            order.Lines.Sum(l => l.Required), order.Lines.Sum(l => l.Ready), order.Lines, messages, order.UpdatedAt);
    }
}

/// <summary>A buyer asks for something from their portal: a note, a call, or a video of the goods.</summary>
public sealed record SendPortalMessageCommand(string Code, ClientMessageType Type, string Message, string? RequestedWhen) : ICommand<ErrorOr<Success>>;

public sealed class SendPortalMessageValidator : AbstractValidator<SendPortalMessageCommand>
{
    public SendPortalMessageValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(ClientMessage.MaxMessageLength);
        RuleFor(x => x.RequestedWhen).MaximumLength(80);
    }
}

public sealed class SendPortalMessageHandler(IAppDbContext db, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<SendPortalMessageCommand, ErrorOr<Success>>
{
    /// <summary>A portal is unauthenticated: cap how many open requests one order can pile up.</summary>
    private const int MaxOpenPerOrder = 30;

    public async ValueTask<ErrorOr<Success>> Handle(SendPortalMessageCommand command, CancellationToken ct)
    {
        if (!PortalRules.LooksValid(command.Code))
            return PortalRules.UnknownCode;

        var code = command.Code.Trim();
        var order = await db.ClientOrders.AsNoTracking().FirstOrDefaultAsync(o => o.LoginCode == code, ct);
        if (order is null)
            return PortalRules.UnknownCode;
        if (await db.ClientMessages.CountAsync(m => m.ClientOrderId == order.Id && !m.IsDone, ct) >= MaxOpenPerOrder)
            return Error.Validation(description: "You have many open requests already — we'll get back to you on those first.");

        db.ClientMessages.Add(new ClientMessage(order, command.Type, command.Message, command.RequestedWhen, clock.UtcNow));
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.ClientMessages, RealtimeAudience.Owner, null, ct);
        return Result.Success;
    }
}

// ---------- Owner side ----------

public sealed record ClientMessageDto(Guid Id, Guid ClientOrderId, string OrderName, string? ClientName, ClientMessageType Type, string Message,
    string? RequestedWhen, bool IsDone, DateTimeOffset? DoneAt, DateTimeOffset CreatedAt);

public sealed record GetClientMessagesQuery(bool OpenOnly) : IQuery<IReadOnlyList<ClientMessageDto>>;

public sealed class GetClientMessagesHandler(IAppDbContext db) : IQueryHandler<GetClientMessagesQuery, IReadOnlyList<ClientMessageDto>>
{
    public async ValueTask<IReadOnlyList<ClientMessageDto>> Handle(GetClientMessagesQuery query, CancellationToken ct) =>
        await db.ClientMessages.AsNoTracking()
            .Where(m => !query.OpenOnly || !m.IsDone)
            .OrderBy(m => m.IsDone).ThenByDescending(m => m.CreatedAt)
            .Take(200)
            .Select(m => new ClientMessageDto(m.Id, m.ClientOrderId, m.OrderName, m.ClientName, m.Type, m.Message, m.RequestedWhen, m.IsDone, m.DoneAt, m.CreatedAt))
            .ToListAsync(ct);
}

public sealed record SetClientMessageDoneCommand(Guid Id, bool Done) : ICommand<ErrorOr<Success>>;

public sealed class SetClientMessageDoneHandler(IAppDbContext db, ICurrentUser user, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<SetClientMessageDoneCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetClientMessageDoneCommand command, CancellationToken ct)
    {
        var message = await db.ClientMessages.FirstOrDefaultAsync(m => m.Id == command.Id, ct);
        if (message is null)
            return Error.NotFound(description: "Message not found.");

        message.SetDone(command.Done, user.RequireUserId(), clock.UtcNow);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.ClientMessages, RealtimeAudience.Owner, null, ct);
        return Result.Success;
    }
}
