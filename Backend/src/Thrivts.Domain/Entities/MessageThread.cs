using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>A message thread between a participant (buyer/seller/agency) and admin, tied to a
/// requirement, deal, bid, or offer. The individual messages live in a separate `messages` table
/// not yet modelled here (see the 15 newly-discovered tables noted in the schema sync report).</summary>
public class MessageThread : BaseEntity, IAggregateRoot
{
    public Guid? RequirementId { get; private set; }
    public Guid? DealId { get; private set; }
    public Guid? SellerResponseId { get; private set; }
    public Guid? OfferId { get; private set; }

    public UserRole ParticipantRole { get; private set; }
    public Guid ParticipantId { get; private set; }
    public string? Subject { get; private set; }

    public bool IsOpen { get; private set; } = true;
    public DateTimeOffset? LastMessageAt { get; private set; }
    public bool UnreadForAdmin { get; private set; }
    public bool UnreadForParticipant { get; private set; }
    public int UnreadCount { get; private set; }

    private MessageThread()
    {
        // EF Core
    }

    public MessageThread(UserRole participantRole, Guid participantId, Guid? requirementId = null,
        Guid? dealId = null, Guid? sellerResponseId = null, Guid? offerId = null, string? subject = null)
    {
        ParticipantRole = participantRole;
        ParticipantId = participantId;
        RequirementId = requirementId;
        DealId = dealId;
        SellerResponseId = sellerResponseId;
        OfferId = offerId;
        Subject = subject;
    }

    public void RecordIncomingFromParticipant(DateTimeOffset occurredAt)
    {
        LastMessageAt = occurredAt;
        UnreadForAdmin = true;
        UnreadCount += 1;
    }

    public void RecordIncomingFromAdmin(DateTimeOffset occurredAt)
    {
        LastMessageAt = occurredAt;
        UnreadForParticipant = true;
    }

    public void MarkReadByAdmin()
    {
        UnreadForAdmin = false;
        UnreadCount = 0;
    }

    public void MarkReadByParticipant() => UnreadForParticipant = false;

    public void Close() => IsOpen = false;

    public void Reopen() => IsOpen = true;
}
