using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Gate;

namespace Thrivts.Api.Controllers.V1.Staff;

[Route("api/v{version:apiVersion}/gate")]
public sealed class GateController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>An arrival opens a new lot (VEWL serial); an exit records a GATE slip. Sent with an
    /// Idempotency-Key so a retry on a flaky connection never creates a second lot.</summary>
    [HttpPost("entries")]
    [Idempotent]
    [Authorize(Policy = Permissions.GateWrite)]
    public async Task<IActionResult> Record([FromBody] RecordGateEntryCommand command, CancellationToken ct) =>
        ToCreated(await Mediator.Send(command, ct));

    /// <summary>Preview of the next serials for the form header — never reserves them.</summary>
    [HttpGet("next-serials")]
    [Authorize(Policy = Permissions.GateWrite)]
    public async Task<NextGateSerialsDto> NextSerials(CancellationToken ct) =>
        await Mediator.Send(new GetNextGateSerialsQuery(), ct);

    [HttpGet("entries")]
    [Authorize(Policy = Permissions.GateRead)]
    public async Task<CursorPage<GateEntryDto>> History([FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetGateHistoryQuery(cursor, size), ct);
}
