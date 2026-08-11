using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Audit;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/audit-log")]
public class AdminAuditController : AdminControllerBase
{
    public AdminAuditController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int take, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAuditLogQuery(take), cancellationToken);
        return ToResponse(result);
    }
}
