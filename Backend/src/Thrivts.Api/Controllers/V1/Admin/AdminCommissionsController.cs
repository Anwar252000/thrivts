using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Commissions;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/commissions")]
public class AdminCommissionsController : AdminControllerBase
{
    public AdminCommissionsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] CommissionStatus? status, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCommissionsQuery(status), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("fee-revenue")]
    public async Task<IActionResult> GetFeeRevenue(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPlatformFeeRevenueQuery(), cancellationToken);
        return ToResponse(result);
    }
}
