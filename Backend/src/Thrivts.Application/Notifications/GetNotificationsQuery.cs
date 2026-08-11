using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Notifications;

/// <summary>Powers every role's bell — scoped to the caller's own notifications only.</summary>
public sealed record GetNotificationsQuery(bool? UnreadOnly, int Take) : IQuery<ErrorOr<List<NotificationDto>>>;

public sealed record NotificationDto(
    Guid Id, NotificationChannel Channel, string Title, string Body, string? RefType, Guid? RefId,
    bool IsRead, DateTimeOffset? ReadAt, DateTimeOffset CreatedAt);

public sealed class GetNotificationsQueryHandler : IQueryHandler<GetNotificationsQuery, ErrorOr<List<NotificationDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetNotificationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<NotificationDto>>> Handle(GetNotificationsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var notifications = _db.Notifications.AsNoTracking().Where(n => n.RecipientId == _currentUser.UserId);
        if (query.UnreadOnly == true)
            notifications = notifications.Where(n => !n.IsRead);

        var take = query.Take is > 0 and <= 200 ? query.Take : 50;

        var result = await notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto(n.Id, n.Channel, n.Title, n.Body, n.RefType, n.RefId, n.IsRead, n.ReadAt, n.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
