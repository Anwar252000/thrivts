using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Approvals;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/approvals")]
public class AdminApprovalsController : AdminControllerBase
{
    public AdminApprovalsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetPending([FromQuery] UserRole? role, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPendingApprovalsQuery(role), cancellationToken);
        return ToResponse(result);
    }
}
