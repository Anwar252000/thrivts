using Thrivts.Domain.Common;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One message in the internal admin/ops team chat (the live schema's ops_chat table — an
/// internal tool for the Thrivts team, not buyer/seller/agency facing). Id is a bigint, not a uuid.
/// </summary>
public class OpsChatMessage : IAggregateRoot
{
    public long Id { get; private set; }
    public string Sender { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public long? Ts { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private OpsChatMessage()
    {
        // EF Core
    }

    public OpsChatMessage(string sender, string body, long? ts = null)
    {
        Sender = sender;
        Body = body;
        Ts = ts;
    }
}
