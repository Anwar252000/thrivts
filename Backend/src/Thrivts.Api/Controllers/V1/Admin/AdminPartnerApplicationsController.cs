using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.PartnerApplications;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/partner-applications")]
public class AdminPartnerApplicationsController : AdminControllerBase
{
    public AdminPartnerApplicationsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPartnerApplicationsQuery(status), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("{applicationId:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid applicationId, [FromBody] SetPartnerApplicationStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetPartnerApplicationStatusCommand(applicationId, request.Approve, request.InfluencerId), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record SetPartnerApplicationStatusRequest(bool Approve, Guid? InfluencerId);
