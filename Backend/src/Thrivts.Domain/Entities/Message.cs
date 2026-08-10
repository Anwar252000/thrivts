using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>One message within a MessageThread (the live schema's messages table).</summary>
public class Message : BaseEntity, IAggregateRoot
{
    public Guid ThreadId { get; private set; }
    public Guid SenderId { get; private set; }
    public MessageSenderType SenderType { get; private set; }
    public string Body { get; private set; } = default!;
    public string? AttachmentsJson { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private Message()
    {
        // EF Core
    }

    public Message(Guid threadId, Guid senderId, MessageSenderType senderType, string body, string? attachmentsJson = null)
    {
        ThreadId = threadId;
        SenderId = senderId;
        SenderType = senderType;
        Body = body;
        AttachmentsJson = attachmentsJson;
    }

    public void MarkRead(DateTimeOffset occurredAt)
    {
        IsRead = true;
        ReadAt ??= occurredAt;
    }
}
