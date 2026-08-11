using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Disputes;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/disputes")]
public class AdminDisputesController : AdminControllerBase
{
    public AdminDisputesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DisputeStatus? status, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDisputesQuery(status), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("{disputeId:guid}/investigate")]
    public async Task<IActionResult> BeginInvestigation(Guid disputeId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new BeginDisputeInvestigationCommand(disputeId), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{disputeId:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid disputeId, [FromBody] ResolveDisputeRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new ResolveDisputeCommand(disputeId, request.Resolution, request.ResolutionNotes, request.RefundAmountUsd), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{disputeId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid disputeId, [FromBody] RejectDisputeRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RejectDisputeCommand(disputeId, request.ResolutionNotes), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record ResolveDisputeRequest(string Resolution, string? ResolutionNotes, decimal RefundAmountUsd);
public record RejectDisputeRequest(string? ResolutionNotes);
