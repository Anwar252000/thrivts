using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Identity;
using Thrivts.Domain.Orders;

namespace Thrivts.Application.Features.Owner;

// ---------- VEW Clients (owner view of orders) ----------

public sealed record OwnerOrderLineDto(Guid Id, string ItemName, string? Grade, string? Category, int Required, int Ready, int ClientReady, OrderLineStatus Status, string? Note, long Seq);

public sealed record OrderPaymentDto(Guid Id, string? Label, decimal Amount, bool IsPaid, DateOnly? PaidOn);

public sealed record OwnerOrderDto(
    Guid Id, string Name, string? ClientName, ClientOrderStatus Status, DateOnly? Deadline, bool IsCompleted, string? LoginCode, decimal TotalValue,
    int Required, int ClientReady, decimal Paid, IReadOnlyList<OwnerOrderLineDto> Lines, IReadOnlyList<OrderPaymentDto> Payments,
    string? DestinationCountry, DateOnly? OrderReceivedDate, DateOnly? DispatchDate, int? Boxes, decimal? WeightKg);

public sealed record GetOwnerOrdersQuery(bool Completed) : IQuery<IReadOnlyList<OwnerOrderDto>>;

public sealed class GetOwnerOrdersHandler(IAppDbContext db) : IQueryHandler<GetOwnerOrdersQuery, IReadOnlyList<OwnerOrderDto>>
{
    public async ValueTask<IReadOnlyList<OwnerOrderDto>> Handle(GetOwnerOrdersQuery query, CancellationToken ct) =>
        await db.ClientOrders.AsNoTracking()
            .Where(o => o.IsCompleted == query.Completed)
            .OrderByDescending(o => o.CreatedAt)
            .Take(300)
            .Select(o => new OwnerOrderDto(
                o.Id, o.Name, o.ClientName, o.Status, o.Deadline, o.IsCompleted, o.LoginCode, o.TotalValue,
                o.Lines.Sum(l => l.Required), o.Lines.Sum(l => l.ClientReady), o.Payments.Where(p => p.IsPaid).Sum(p => p.Amount),
                o.Lines.OrderBy(l => l.Seq).Select(l => new OwnerOrderLineDto(l.Id, l.ItemName, l.Grade, l.Category, l.Required, l.Ready, l.ClientReady, l.Status, l.Note, l.Seq)).ToList(),
                o.Payments.OrderBy(p => p.CreatedAt).Select(p => new OrderPaymentDto(p.Id, p.Label, p.Amount, p.IsPaid, p.PaidOn)).ToList(),
                o.DestinationCountry, o.OrderReceivedDate, o.DispatchDate, o.Boxes, o.WeightKg))
            .ToListAsync(ct);
}

public sealed record SetOrderOwnerFieldsCommand(Guid Id, decimal TotalValue, string? LoginCode) : ICommand<ErrorOr<Success>>;

public sealed class SetOrderOwnerFieldsValidator : AbstractValidator<SetOrderOwnerFieldsCommand>
{
    public SetOrderOwnerFieldsValidator()
    {
        RuleFor(x => x.TotalValue).InclusiveBetween(0, 10_000_000_000);
        // The code is the buyer's only credential for their portal: long enough not to be guessed.
        RuleFor(x => x.LoginCode!).Must(c => Portal.PortalRules.LooksValid(c.Trim()))
            .When(x => !string.IsNullOrWhiteSpace(x.LoginCode))
            .WithMessage($"Login code must be {Portal.PortalRules.MinCodeLength}–{Portal.PortalRules.MaxCodeLength} letters, digits, - or _.");
    }
}

public sealed class SetOrderOwnerFieldsHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<SetOrderOwnerFieldsCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetOrderOwnerFieldsCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.FirstOrDefaultAsync(o => o.Id == command.Id, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");

        var code = string.IsNullOrWhiteSpace(command.LoginCode) ? null : command.LoginCode.Trim();
        if (code is not null && await db.ClientOrders.AnyAsync(o => o.LoginCode == code && o.Id != order.Id, ct))
            return Error.Conflict(description: "Another order already uses that login code.");

        order.SetOwnerFields(command.TotalValue, code);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.Owner, null, ct);
        return Result.Success;
    }
}

/// <summary>The client-facing "ready" figure on a line — allowed to run ahead of the real count.</summary>
public sealed record SetLineClientReadyCommand(Guid OrderId, Guid LineId, int ClientReady) : ICommand<ErrorOr<Success>>;

public sealed class SetLineClientReadyHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<SetLineClientReadyCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetLineClientReadyCommand command, CancellationToken ct)
    {
        var line = await db.OrderLines.FirstOrDefaultAsync(l => l.Id == command.LineId && l.ClientOrderId == command.OrderId, ct);
        if (line is null)
            return Error.NotFound(description: "Line not found.");

        line.SetClientReady(command.ClientReady);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Orders, RealtimeAudience.Owner, null, ct);
        return Result.Success;
    }
}

public sealed record SaveOrderPaymentCommand(Guid OrderId, Guid? PaymentId, string? Label, decimal Amount, bool IsPaid, DateOnly? PaidOn) : ICommand<ErrorOr<Guid>>;

public sealed class SaveOrderPaymentValidator : AbstractValidator<SaveOrderPaymentCommand>
{
    public SaveOrderPaymentValidator()
    {
        RuleFor(x => x.Label).MaximumLength(120);
        RuleFor(x => x.Amount).InclusiveBetween(0, 10_000_000_000);
    }
}

public sealed class SaveOrderPaymentHandler(IAppDbContext db, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<SaveOrderPaymentCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(SaveOrderPaymentCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");

        var paidOn = command.IsPaid ? command.PaidOn ?? clock.Today : (DateOnly?)null;
        ClientOrderPayment payment;
        if (command.PaymentId is { } id)
        {
            payment = order.Payments.FirstOrDefault(p => p.Id == id) ?? throw new Domain.Exceptions.DomainException("Payment not found.");
            payment.Update(command.Label?.Trim(), command.Amount, command.IsPaid, paidOn);
        }
        else
        {
            payment = order.AddPayment(command.Label?.Trim(), command.Amount, command.IsPaid, paidOn);
        }

        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Finance, RealtimeAudience.Owner, null, ct);
        return payment.Id;
    }
}

public sealed record DeleteOrderPaymentCommand(Guid OrderId, Guid PaymentId) : ICommand<ErrorOr<Success>>;

public sealed class DeleteOrderPaymentHandler(IAppDbContext db) : ICommandHandler<DeleteOrderPaymentCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteOrderPaymentCommand command, CancellationToken ct)
    {
        var order = await db.ClientOrders.Include(o => o.Payments).FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);
        if (order is null)
            return Error.NotFound(description: "Order not found.");

        order.RemovePayment(command.PaymentId);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

// ---------- Approvals & goals ----------

public sealed record ApprovalDto(Guid Id, string FromName, string Description, string? ValueText, ApprovalStatus Status, DateTimeOffset CreatedAt);

public sealed record GoalDto(Guid Id, GoalPeriod Period, string Label, string? Target, int Progress);

public sealed record ApprovalsAndGoalsDto(IReadOnlyList<ApprovalDto> Approvals, IReadOnlyList<GoalDto> Goals);

public sealed record GetApprovalsAndGoalsQuery : IQuery<ApprovalsAndGoalsDto>;

public sealed class GetApprovalsAndGoalsHandler(IAppDbContext db) : IQueryHandler<GetApprovalsAndGoalsQuery, ApprovalsAndGoalsDto>
{
    public async ValueTask<ApprovalsAndGoalsDto> Handle(GetApprovalsAndGoalsQuery query, CancellationToken ct)
    {
        var approvals = await db.Approvals.AsNoTracking().OrderBy(a => a.Status).ThenByDescending(a => a.CreatedAt).Take(200)
            .Select(a => new ApprovalDto(a.Id, a.FromName, a.Description, a.ValueText, a.Status, a.CreatedAt)).ToListAsync(ct);
        var goals = await db.Goals.AsNoTracking().OrderBy(g => g.CreatedAt)
            .Select(g => new GoalDto(g.Id, g.Period, g.Label, g.Target, g.Progress)).ToListAsync(ct);
        return new ApprovalsAndGoalsDto(approvals, goals);
    }
}

public sealed record SubmitApprovalCommand(string Description, string? ValueText) : ICommand<Guid>;

public sealed class SubmitApprovalValidator : AbstractValidator<SubmitApprovalCommand>
{
    public SubmitApprovalValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.ValueText).MaximumLength(60);
    }
}

public sealed class SubmitApprovalHandler(IAppDbContext db, ICurrentUser user, IRealtimeNotifier realtime) : ICommandHandler<SubmitApprovalCommand, Guid>
{
    public async ValueTask<Guid> Handle(SubmitApprovalCommand command, CancellationToken ct)
    {
        var approval = new Approval(user.UserId, user.Name ?? "Team", command.Description, command.ValueText?.Trim());
        db.Approvals.Add(approval);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Escalations, RealtimeAudience.Owner, null, ct);
        return approval.Id;
    }
}

public sealed record DecideApprovalCommand(Guid Id, bool Approve) : ICommand<ErrorOr<Success>>;

public sealed class DecideApprovalHandler(IAppDbContext db, ICurrentUser user, IClock clock) : ICommandHandler<DecideApprovalCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DecideApprovalCommand command, CancellationToken ct)
    {
        var approval = await db.Approvals.FirstOrDefaultAsync(a => a.Id == command.Id, ct);
        if (approval is null)
            return Error.NotFound(description: "Approval not found.");

        approval.Decide(command.Approve, user.RequireUserId(), clock.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

public sealed record SaveGoalCommand(Guid? Id, GoalPeriod Period, string Label, string? Target, int Progress) : ICommand<ErrorOr<Guid>>;

public sealed class SaveGoalValidator : AbstractValidator<SaveGoalCommand>
{
    public SaveGoalValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Target).MaximumLength(200);
        RuleFor(x => x.Progress).InclusiveBetween(0, 100);
    }
}

public sealed class SaveGoalHandler(IAppDbContext db) : ICommandHandler<SaveGoalCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(SaveGoalCommand command, CancellationToken ct)
    {
        Goal goal;
        if (command.Id is { } id)
        {
            var existing = await db.Goals.FirstOrDefaultAsync(g => g.Id == id, ct);
            if (existing is null)
                return Error.NotFound(description: "Goal not found.");
            existing.Update(command.Period, command.Label, command.Target?.Trim(), command.Progress);
            goal = existing;
        }
        else
        {
            goal = new Goal(command.Period, command.Label, command.Target?.Trim(), command.Progress);
            db.Goals.Add(goal);
        }

        await db.SaveChangesAsync(ct);
        return goal.Id;
    }
}

public sealed record DeleteGoalCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class DeleteGoalHandler(IAppDbContext db) : ICommandHandler<DeleteGoalCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteGoalCommand command, CancellationToken ct)
    {
        var goal = await db.Goals.FirstOrDefaultAsync(g => g.Id == command.Id, ct);
        if (goal is null)
            return Error.NotFound(description: "Goal not found.");

        db.Goals.Remove(goal);
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

// ---------- Users ----------

public sealed record UserDto(Guid Id, string DisplayName, string Title, UserRole Role, AppKind App, string Color, bool IsActive, bool IsLockedOut, DateTimeOffset? LastLoginAt, int SortOrder);

public sealed record GetUsersQuery : IQuery<IReadOnlyList<UserDto>>;

public sealed class GetUsersHandler(IAppDbContext db, IClock clock) : IQueryHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async ValueTask<IReadOnlyList<UserDto>> Handle(GetUsersQuery query, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return await db.Users.AsNoTracking().OrderBy(u => u.App).ThenBy(u => u.SortOrder)
            .Select(u => new UserDto(u.Id, u.DisplayName, u.Title, u.Role, u.App, u.Color, u.IsActive, u.LockoutUntil != null && u.LockoutUntil > now, u.LastLoginAt, u.SortOrder))
            .ToListAsync(ct);
    }
}

public sealed record SaveUserCommand(Guid? Id, string DisplayName, string Title, UserRole Role, string Color, string? Pin, int SortOrder) : ICommand<ErrorOr<Guid>>;

public sealed class SaveUserValidator : AbstractValidator<SaveUserCommand>
{
    public SaveUserValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(AppUser.MaxDisplayNameLength);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").WithMessage("Colour must look like #1AA767.");
        RuleFor(x => x.Pin).NotEmpty().When(x => x.Id is null).WithMessage("Set a PIN for the new user.");
        RuleFor(x => x.Pin).Must((cmd, pin) => pin is null || (pin.Length == UserRoles.PinLengthFor(cmd.Role) && pin.All(char.IsAsciiDigit) && pin.Distinct().Count() > 1))
            .WithMessage(cmd => $"PIN must be {UserRoles.PinLengthFor(cmd.Role)} digits and not one digit repeated.");
    }
}

/// <summary>Creating a user or changing their role/PIN. Changing role or PIN signs the user out of
/// every device, so a removed permission takes effect at once.</summary>
public sealed class SaveUserHandler(IAppDbContext db, IPinHasher hasher, ICurrentUser caller, IClock clock) : ICommandHandler<SaveUserCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(SaveUserCommand command, CancellationToken ct)
    {
        AppUser user;
        var revokeSessions = false;
        if (command.Id is { } id)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
            if (existing is null)
                return Error.NotFound(description: "User not found.");
            if (existing.Id == caller.UserId && existing.Role != command.Role)
                return Error.Validation(description: "You can't change your own role.");
            if (existing.Role != command.Role)
                return Error.Validation(description: "A user's role can't change — create a new user instead.");

            existing.Rename(command.DisplayName, command.Title);
            existing.SetColor(command.Color);
            if (command.Pin is not null)
            {
                existing.ChangePin(hasher.Hash(command.Pin));
                revokeSessions = true;
            }

            user = existing;
        }
        else
        {
            user = new AppUser(command.DisplayName, command.Title, command.Role, command.Color, hasher.Hash(command.Pin!), command.SortOrder);
            db.Users.Add(user);
        }

        await db.SaveChangesAsync(ct);
        if (revokeSessions)
            await RevokeAllAsync(db, user.Id, clock.UtcNow, "pin-changed", ct);

        return user.Id;
    }

    internal static Task RevokeAllAsync(IAppDbContext db, Guid userId, DateTimeOffset now, string reason, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.RevokedReason, reason), ct);
}

public sealed record SetUserActiveCommand(Guid Id, bool Active) : ICommand<ErrorOr<Success>>;

public sealed class SetUserActiveHandler(IAppDbContext db, ICurrentUser caller, IClock clock) : ICommandHandler<SetUserActiveCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetUserActiveCommand command, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.Id, ct);
        if (user is null)
            return Error.NotFound(description: "User not found.");
        if (user.Id == caller.UserId && !command.Active)
            return Error.Validation(description: "You can't deactivate yourself.");

        if (command.Active) user.Activate(); else user.Deactivate();
        await db.SaveChangesAsync(ct);
        if (!command.Active)
            await SaveUserHandler.RevokeAllAsync(db, user.Id, clock.UtcNow, "deactivated", ct);

        return Result.Success;
    }
}

// ---------- Audit ----------

public sealed record AuditEntryDto(Guid Id, DateTimeOffset OccurredAt, string? UserName, AuditAction Action, string EntityType, Guid EntityId, string? Changes);

public sealed record GetAuditLogQuery(string? EntityType, Guid? EntityId, string? Cursor, int? Size) : IQuery<CursorPage<AuditEntryDto>>;

public sealed class GetAuditLogHandler(IAppDbContext db) : IQueryHandler<GetAuditLogQuery, CursorPage<AuditEntryDto>>
{
    public async ValueTask<CursorPage<AuditEntryDto>> Handle(GetAuditLogQuery query, CancellationToken ct)
    {
        var size = CursorPage<AuditEntryDto>.NormalizeSize(query.Size);
        var logs = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            logs = logs.Where(a => a.EntityType == query.EntityType);
        if (query.EntityId is { } entityId)
            logs = logs.Where(a => a.EntityId == entityId);
        if (Cursor.Decode(query.Cursor) is { } c)
            logs = logs.Where(a => a.OccurredAt < c.At || (a.OccurredAt == c.At && a.Id.CompareTo(c.Id) < 0));

        var page = await logs.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id).Take(size)
            .Select(a => new AuditEntryDto(a.Id, a.OccurredAt, a.UserName, a.Action, a.EntityType, a.EntityId, a.Changes))
            .ToListAsync(ct);
        var next = page.Count == size ? new Cursor(page[^1].OccurredAt, page[^1].Id).Encode() : null;
        return new CursorPage<AuditEntryDto>(page, next);
    }
}
