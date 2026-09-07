using System.Text.Json;
using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;

namespace Thrivts.Application.Common.Behaviors;

/// <summary>
/// Writes an audit_log row for every successfully-handled mutating command from the Admin, Buyers,
/// or Sellers namespaces — replaces the old client-side logAudit() calls sprinkled through
/// admin.html AND buyer.html (e.g. requirement_posted, dispute_raised were both buyer-initiated
/// audit_log inserts in the old system, not admin-only). Auth namespace (login/register) is
/// deliberately excluded — the old system never audit-logged those. Queries and failed
/// (ErrorOr IsError) commands are not logged.
/// </summary>
public class AuditLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private static readonly string[] AuditedNamespacePrefixes =
        ["Thrivts.Application.Admin", "Thrivts.Application.Buyers", "Thrivts.Application.Sellers"];

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditLoggingBehavior(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(message, cancellationToken);

        var messageType = typeof(TMessage);
        var isAuditedCommand = messageType.Namespace is not null
            && AuditedNamespacePrefixes.Any(prefix => messageType.Namespace.StartsWith(prefix, StringComparison.Ordinal))
            && messageType.Name.EndsWith("Command", StringComparison.Ordinal);

        if (isAuditedCommand && _currentUser.UserId is not null && response is not IErrorOr { IsError: true })
        {
            var detailsJson = JsonSerializer.Serialize(message);
            var log = new AuditLog(
                action: messageType.Name,
                entityType: messageType.Name,
                actorId: _currentUser.UserId,
                actorRole: _currentUser.Role,
                detailsJson: detailsJson);

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
