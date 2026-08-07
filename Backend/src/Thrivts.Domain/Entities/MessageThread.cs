using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// A message thread tied to a deal or requirement. NOTE: inferred — the message_threads table
/// was named in the migration plan's entity list but its schema was not in the exported SQL set
/// (no messaging UI was found in the audited portals either). Verify this is still in scope
/// before building against it.
/// </summary>
public class MessageThread : BaseEntity, IAggregateRoot
{
    public Guid? DealId { get; private set; }
    public Guid? RequirementId { get; private set; }
    public Guid CreatedByProfileId { get; private set; }
    public bool IsClosed { get; private set; }

    private MessageThread()
    {
        // EF Core
    }

    public MessageThread(Guid createdByProfileId, Guid? dealId = null, Guid? requirementId = null)
    {
        CreatedByProfileId = createdByProfileId;
        DealId = dealId;
        RequirementId = requirementId;
    }

    public void Close() => IsClosed = true;
}
