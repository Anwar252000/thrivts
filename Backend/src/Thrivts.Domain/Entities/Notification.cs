using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per notification (the live schema's notifications table, PART8A_notifications_table.sql
/// — column set confirmed exactly against that migration). Every write here is also what feeds
/// the thrivts-notify Edge Function -> Resend email; that fan-out becomes an IEmailSender call
/// from the Application layer once this replaces push_notification.
/// </summary>
public class Notification : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public NotificationAudience Audience { get; private set; }
    public string Kind { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Body { get; private set; }
    public string? LinkType { get; private set; }
    public Guid? LinkId { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification()
    {
        // EF Core
    }

    public Notification(Guid userId, NotificationAudience audience, string kind, string title,
        string? body = null, string? linkType = null, Guid? linkId = null)
    {
        UserId = userId;
        Audience = audience;
        Kind = kind;
        Title = title;
        Body = body;
        LinkType = linkType;
        LinkId = linkId;
    }

    public void MarkRead() => ReadAt ??= DateTimeOffset.UtcNow;
}
