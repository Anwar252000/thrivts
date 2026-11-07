using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Lots;

namespace Thrivts.Api.Controllers.V1.Staff;

/// <summary>The lot pipeline: count → grade → supervisor signs → inventory manager co-signs.
/// Each step is its own endpoint so the permission for it is explicit and auditable.</summary>
[Route("api/v{version:apiVersion}/lots")]
public sealed class LotsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    [Authorize(Policy = Permissions.LotsRead)]
    public async Task<CursorPage<LotSummaryDto>> List([FromQuery] LotScope scope, [FromQuery] string? search, [FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetLotsQuery(scope, search, cursor, size), ct);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.LotsRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetLotQuery(id), ct));

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = Permissions.LotsCount)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddLotItemRequest request, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(new AddLotItemCommand(id, request.ItemName, request.Received), ct));

    [HttpPut("{id:guid}/items/{lineId:guid}")]
    [Authorize(Policy = Permissions.LotsCount)]
    public async Task<IActionResult> SetReceived(Guid id, Guid lineId, [FromBody] SetReceivedRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetLotItemReceivedCommand(id, lineId, request.Received), ct));

    [HttpDelete("{id:guid}/items/{lineId:guid}")]
    [Authorize(Policy = Permissions.LotsCount)]
    public async Task<IActionResult> RemoveItem(Guid id, Guid lineId, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new RemoveLotItemCommand(id, lineId), ct));

    [HttpPost("{id:guid}/count/finalize")]
    [Authorize(Policy = Permissions.LotsCount)]
    public async Task<IActionResult> FinalizeCount(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new FinalizeCountCommand(id), ct));

    [HttpPost("{id:guid}/count/reopen")]
    [Authorize(Policy = Permissions.LotsCount)]
    public async Task<IActionResult> ReopenCount(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new ReopenCountCommand(id), ct));

    /// <summary>Saves the grading sheet — as a draft while grading is in progress.</summary>
    [HttpPut("{id:guid}/grades")]
    [Authorize(Policy = Permissions.LotsCount)]
    public async Task<IActionResult> SaveGrades(Guid id, [FromBody] SaveGradesRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SaveGradesCommand(id, request.Lines, request.IsDraft), ct));

    [HttpPost("{id:guid}/sign")]
    [Idempotent]
    [Authorize(Policy = Permissions.LotsCount)]
    public async Task<IActionResult> Sign(Guid id, [FromBody] SignLotRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SignLotCommand(id, request.Signature, request.Lines), ct));

    [HttpPost("{id:guid}/unlock")]
    [Authorize(Policy = Permissions.LotsCosign)]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new UnlockLotCommand(id), ct));

    [HttpPost("{id:guid}/send-back")]
    [Authorize(Policy = Permissions.LotsCosign)]
    public async Task<IActionResult> SendBack(Guid id, [FromBody] SendBackRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SendBackLotCommand(id, request.Reason), ct));

    /// <summary>Co-signing finalizes the lot and books its graded pieces into stock.</summary>
    [HttpPost("{id:guid}/cosign")]
    [Idempotent]
    [Authorize(Policy = Permissions.LotsCosign)]
    public async Task<IActionResult> Cosign(Guid id, [FromBody] CosignRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new CosignLotCommand(id, request.Signature), ct));

    /// <summary>Staff may delete only lots that are not finalized; the owner app has its own route for finalized ones.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.LotsCosign)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteLotCommand(id, AllowFinalized: false), ct));
}

public sealed record AddLotItemRequest(string ItemName, int Received);

public sealed record SetReceivedRequest(int Received);

public sealed record SaveGradesRequest(IReadOnlyList<GradeLine> Lines, bool IsDraft);

public sealed record SignLotRequest(string Signature, IReadOnlyList<GradeLine>? Lines);

public sealed record SendBackRequest(string? Reason);

public sealed record CosignRequest(string Signature);
