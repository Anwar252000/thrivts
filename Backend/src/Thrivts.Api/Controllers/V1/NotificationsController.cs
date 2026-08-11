using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Notifications;

namespace Thrivts.Api.Controllers.V1;

/// <summary>Any authenticated role's own notification bell — buyer, seller, agency, or admin.
/// Every query/command here scopes to the caller's own notifications (never a client-supplied id).</summary>
[Route("api/v{version:apiVersion}/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    public NotificationsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool? unreadOnly, [FromQuery] int take, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetNotificationsQuery(unreadOnly, take), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("mark-read")]
    public async Task<IActionResult> MarkRead([FromBody] MarkNotificationsReadRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new MarkNotificationsReadCommand(request.NotificationIds), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record MarkNotificationsReadRequest(Guid[]? NotificationIds);
