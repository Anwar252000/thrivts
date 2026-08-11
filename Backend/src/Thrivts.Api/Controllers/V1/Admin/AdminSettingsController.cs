using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Settings;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/settings")]
public class AdminSettingsController : AdminControllerBase
{
    public AdminSettingsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("fee-config")]
    public async Task<IActionResult> GetFeeConfig(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPlatformFeeConfigQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPut("fee-config")]
    public async Task<IActionResult> UpdateFeeConfig([FromBody] UpdatePlatformFeeConfigRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdatePlatformFeeConfigCommand(request.FeePerPcUsd, request.PkrReference), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record UpdatePlatformFeeConfigRequest(decimal FeePerPcUsd, string PkrReference);
