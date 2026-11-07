using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Identity;
using Thrivts.Infrastructure.Identity;

namespace Thrivts.Api.Realtime;

/// <summary>
/// One hub, two groups. Clients only ever receive "topic X changed" signals (no data), then refetch
/// through the normal authorized endpoints — so a staff device can never receive owner-only data
/// over the socket, by construction.
/// </summary>
[Authorize]
public sealed class LiveHub : Hub
{
    public const string Path = "/hubs/live";
    public const string ChangedMethod = "changed";

    public static string GroupFor(AppKind app) => app == AppKind.Owner ? "owner" : "staff";

    public override async Task OnConnectedAsync()
    {
        var app = Context.User?.FindFirst(JwtOptions.AppClaim)?.Value;
        if (Enum.TryParse<AppKind>(app, out var kind))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(kind));

        await base.OnConnectedAsync();
    }
}

public sealed class SignalRRealtimeNotifier(IHubContext<LiveHub> hub, ILogger<SignalRRealtimeNotifier> logger) : IRealtimeNotifier
{
    public async Task PublishAsync(string topic, RealtimeAudience audience, object? payload, CancellationToken cancellationToken)
    {
        try
        {
            var message = new { topic, payload };
            var clients = audience switch
            {
                RealtimeAudience.Staff => hub.Clients.Group(LiveHub.GroupFor(AppKind.Staff)),
                RealtimeAudience.Owner => hub.Clients.Group(LiveHub.GroupFor(AppKind.Owner)),
                _ => hub.Clients.Groups(LiveHub.GroupFor(AppKind.Staff), LiveHub.GroupFor(AppKind.Owner)),
            };

            await clients.SendAsync(LiveHub.ChangedMethod, message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A failed push must never fail the write that triggered it — clients also refetch on focus.
            logger.LogWarning(ex, "Realtime publish failed for topic {Topic}", topic);
        }
    }
}
