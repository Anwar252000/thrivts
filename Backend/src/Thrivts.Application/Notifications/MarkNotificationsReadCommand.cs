using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Notifications;

/// <summary>
/// Replaces mark_notifications_read. Not admin-only — any authenticated user marks their OWN
/// notifications read; NotificationIds null means "mark all of mine". Lives outside the Admin.*
/// namespace so AuditLoggingBehavior does not log it (this isn't an admin action).
/// </summary>
public sealed record MarkNotificationsReadCommand(Guid[]? NotificationIds) : ICommand<ErrorOr<Success>>;

public sealed class MarkNotificationsReadCommandHandler : ICommandHandler<MarkNotificationsReadCommand, ErrorOr<Success>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public MarkNotificationsReadCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async ValueTask<ErrorOr<Success>> Handle(MarkNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var query = _db.Notifications.Where(n => n.RecipientId == _currentUser.UserId && !n.IsRead);
        if (command.NotificationIds is { Length: > 0 })
            query = query.Where(n => command.NotificationIds.Contains(n.Id));

        var notifications = await query.ToListAsync(cancellationToken);
        foreach (var notification in notifications)
            notification.MarkRead(_clock.UtcNow);

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
