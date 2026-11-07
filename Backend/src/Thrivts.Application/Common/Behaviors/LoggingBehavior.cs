using System.Diagnostics;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Thrivts.Application.Common.Behaviors;

/// <summary>Times every Command/Query. Payloads are never logged — they can carry PINs, tokens or
/// prices. Anything slower than half a second is logged as a warning so slow queries surface
/// before users complain.</summary>
public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private const int SlowThresholdMs = 500;

    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await next(message, cancellationToken);
        var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;

        if (elapsedMs > SlowThresholdMs)
            logger.LogWarning("Slow request {MessageName} took {ElapsedMs:0} ms", typeof(TMessage).Name, elapsedMs);
        else if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Handled {MessageName} in {ElapsedMs:0} ms", typeof(TMessage).Name, elapsedMs);

        return response;
    }
}
