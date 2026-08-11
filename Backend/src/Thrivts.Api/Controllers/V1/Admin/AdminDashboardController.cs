using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Dashboard;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/dashboard")]
public class AdminDashboardController : AdminControllerBase
{
    public AdminDashboardController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDashboardStatsQuery(), cancellationToken);
        return ToResponse(result);
    }
}
