using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per notification. The live schema has TWO generations of columns on this table:
/// RecipientId/Channel/RefType/RefId/Language (current, NOT NULL recipient) and the original
/// UserId/Audience/Kind/LinkType/LinkId from PART8A_notifications_table.sql (legacy, nullable —
/// RLS reads `coalesce(recipient_id, user_id)`, see THRIVTS_AUDIT_AND_TEST_SCRIPT.md). New code
/// should only ever write the current columns; the legacy ones are kept for existing rows.
/// </summary>
public class Notification : BaseEntity, IAggregateRoot
{
    public Guid RecipientId { get; private set; }
    public NotificationChannel Channel { get; private set; } = NotificationChannel.InApp;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public string? RefType { get; private set; }
    public Guid? RefId { get; private set; }
    public LanguagePref Language { get; private set; } = LanguagePref.En;

    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public string? DeliveryStatus { get; private set; }
    public string? DeliveryError { get; private set; }

    // Legacy (PART8A) columns — read-compatible only, never written by new code.
    public Guid? UserId { get; private set; }
    public string? Audience { get; private set; }
    public string? Kind { get; private set; }
    public string? LinkType { get; private set; }
    public Guid? LinkId { get; private set; }

    private Notification()
    {
        // EF Core
    }

    public Notification(Guid recipientId, string title, string body, NotificationChannel channel = NotificationChannel.InApp,
        string? refType = null, Guid? refId = null, LanguagePref language = LanguagePref.En)
    {
        RecipientId = recipientId;
        Title = title;
        Body = body;
        Channel = channel;
        RefType = refType;
        RefId = refId;
        Language = language;
    }

    public void MarkRead(DateTimeOffset occurredAt)
    {
        IsRead = true;
        ReadAt ??= occurredAt;
    }

    public void MarkSent(DateTimeOffset occurredAt, string deliveryStatus)
    {
        SentAt = occurredAt;
        DeliveryStatus = deliveryStatus;
    }

    public void MarkDeliveryFailed(string error) => DeliveryError = error;
}
