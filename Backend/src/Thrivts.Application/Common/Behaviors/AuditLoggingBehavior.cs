using System.Text.Json;
using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;

namespace Thrivts.Application.Common.Behaviors;

/// <summary>
/// Writes an audit_log row for every successfully-handled Admin.*Command — replaces the old
/// client-side logAudit() calls sprinkled through admin.html (easy to forget, easy to bypass).
/// Queries and non-Admin commands are skipped; failed (ErrorOr IsError) commands are not logged.
/// </summary>
public class AuditLoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
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
        var isAdminCommand = messageType.Namespace?.StartsWith("Thrivts.Application.Admin", StringComparison.Ordinal) == true
            && messageType.Name.EndsWith("Command", StringComparison.Ordinal);

        if (isAdminCommand && _currentUser.UserId is not null && response is not IErrorOr { IsError: true })
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
