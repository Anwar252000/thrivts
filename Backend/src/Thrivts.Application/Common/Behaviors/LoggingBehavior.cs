using Mediator;
using Microsoft.Extensions.Logging;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Common.Behaviors;

/// <summary>
/// Structured log line for every Command/Query, tagged with the calling user. Admin/* commands
/// are also picked up here as a cheap audit-trail companion (the real audit_log write happens in
/// AuditLoggingBehavior for Admin commands specifically).
/// </summary>
public class LoggingBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger<LoggingBehavior<TMessage, TResponse>> _logger;
    private readonly ICurrentUserService _currentUser;

    public LoggingBehavior(ILogger<LoggingBehavior<TMessage, TResponse>> logger, ICurrentUserService currentUser)
    {
        _logger = logger;
        _currentUser = currentUser;
    }

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling {MessageName} for user {UserId}",
            typeof(TMessage).Name,
            _currentUser.UserId);

        return await next(message, cancellationToken);
    }
}
