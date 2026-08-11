using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Messages;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/message-threads")]
public class AdminMessagesController : AdminControllerBase
{
    public AdminMessagesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetThreads([FromQuery] bool? unreadOnly, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMessageThreadsQuery(unreadOnly), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("{threadId:guid}/messages")]
    public async Task<IActionResult> GetMessages(Guid threadId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetThreadMessagesQuery(threadId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("{threadId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid threadId, [FromBody] SendAdminMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SendAdminMessageCommand(threadId, request.Body), cancellationToken);
        return ToResponse(result, id => Ok(new { id }));
    }

    [HttpPost("{threadId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid threadId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new MarkThreadReadCommand(threadId), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record SendAdminMessageRequest(string Body);
