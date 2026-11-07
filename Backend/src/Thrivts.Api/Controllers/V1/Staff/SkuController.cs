using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Common;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Communication;
using Thrivts.Application.Features.Sku;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Sku;

namespace Thrivts.Api.Controllers.V1.Staff;

/// <summary>SKU packing: the pool of graded pieces, sacks packed from it, and their dispatch.</summary>
[Route("api/v{version:apiVersion}/sku")]
public sealed class SkuController(IMediator mediator) : ApiControllerBase(mediator)
{
    private const int MaxLabelsPerRequest = 200;

    [HttpGet("pool")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IReadOnlyList<PoolItemDto>> Pool([FromQuery] string? search, CancellationToken ct) =>
        await Mediator.Send(new GetPoolQuery(search), ct);

    [HttpGet("summary")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<SkuSummaryDto> Summary(CancellationToken ct) =>
        await Mediator.Send(new GetSkuSummaryQuery(), ct);

    [HttpGet("sacks")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<PagedResult<SackDto>> Sacks([FromQuery] string? item, [FromQuery] SackDestination? destination, [FromQuery] SackSort sort = SackSort.Number,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        await Mediator.Send(new GetSacksQuery(item, destination, sort, page, pageSize), ct);

    [HttpGet("sacks/facets")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IReadOnlyList<SackItemFacet>> Facets(CancellationToken ct) =>
        await Mediator.Send(new GetSackItemFacetsQuery(), ct);

    [HttpGet("sacks/{id:guid}")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IActionResult> Sack(Guid id, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetSackQuery(id), ct));

    /// <summary>Printable labels for a selection of sacks (POST because the id list can be long).</summary>
    [HttpPost("sacks/labels")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IActionResult> Labels([FromBody] SackIdsRequest request, CancellationToken ct)
    {
        if (request.SackIds is not { Count: > 0 and <= MaxLabelsPerRequest })
            return Problem(title: $"Select between 1 and {MaxLabelsPerRequest} sacks.", statusCode: StatusCodes.Status422UnprocessableEntity);

        return Ok(await Mediator.Send(new GetSackLabelsQuery(request.SackIds), ct));
    }

    /// <summary>Packs N sacks and returns their new codes.</summary>
    [HttpPost("sacks")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Pack([FromBody] PackSacksCommand command, CancellationToken ct) =>
        ToResponse(await Mediator.Send(command, ct), codes => StatusCode(StatusCodes.Status201Created, new { codes }));

    [HttpPost("sacks/{id:guid}/top-up")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> TopUp(Guid id, [FromBody] SackQuantitiesRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new TopUpSackCommand(id, request.A, request.C), ct));

    [HttpPost("sacks/{id:guid}/take-out")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> TakeOut(Guid id, [FromBody] SackQuantitiesRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new TakeOutOfSackCommand(id, request.A, request.C), ct));

    [HttpPut("sacks/{id:guid}/assignment")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignSackRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new AssignSackCommand(id, request.Assignment, request.ClientOrderId, request.OrderLineId, request.NewLine), ct));

    [HttpDelete("sacks/{id:guid}/assignment")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Unassign(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new UnassignSackCommand(id), ct));

    [HttpPost("sacks/ship")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Ship([FromBody] SackIdsRequest request, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new ShipSacksCommand(request.SackIds ?? []), ct), pieces => Ok(new { pieces }));

    /// <summary>Sends loose (unpacked) pieces straight out of the pool.</summary>
    [HttpPost("loose-dispatches")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> DispatchLoose([FromBody] DispatchLooseCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command, ct));

    /// <summary>The SKU tab's activity log (packing, top-ups, assignments, shipments) — the general
    /// activity feed needs AnalyticsRead, which the inventory login doesn't have.</summary>
    [HttpGet("activity")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<CursorPage<ActivityDto>> Activity([FromQuery] string? search, [FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetActivityQuery(ActivityArea.Sku, search, null, null, cursor, size), ct);

    [HttpGet("activity.csv")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public Task ExportActivity(CancellationToken ct) =>
        CsvResults.WriteAsync(Response, "sku-activity.csv", ["When", "Type", "Activity", "By"],
            Mediator.CreateStream(new ExportActivityQuery(ActivityArea.Sku), ct),
            e => [e.OccurredAt.ToOffset(TimeSpan.FromHours(5)), e.Type, e.Text, e.UserName],
            ct);

    [HttpGet("dispatches")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<CursorPage<SkuDispatchDto>> Dispatches([FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetSkuDispatchesQuery(cursor, size), ct);
}

public sealed record SackIdsRequest(IReadOnlyList<Guid>? SackIds);

public sealed record SackQuantitiesRequest(int A, int C);

public sealed record AssignSackRequest(SackAssignment Assignment, Guid? ClientOrderId, Guid? OrderLineId, bool NewLine);
