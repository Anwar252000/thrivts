using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Common;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Dashboards;
using Thrivts.Application.Features.Inventory;
using Thrivts.Domain.Inventory;

namespace Thrivts.Api.Controllers.V1.Staff;

[Route("api/v{version:apiVersion}/inventory")]
public sealed class InventoryController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet("lots")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<PagedResult<InventoryLotDto>> Lots([FromQuery] string? search, [FromQuery] Guid? supplierId, [FromQuery] bool includeCleared = false,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        await Mediator.Send(new GetInventoryLotsQuery(search, supplierId, includeCleared, page, pageSize), ct);

    [HttpGet("summary")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<InventorySummaryDto> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        await Mediator.Send(new GetInventorySummaryQuery(from, to), ct);

    [HttpGet("dashboard")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<InventoryDashboardDto> Dashboard([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken ct) =>
        await Mediator.Send(new GetInventoryDashboardQuery(from, to), ct);

    /// <summary>Available stock per item/grade for one lot — feeds the dispatch line picker.</summary>
    [HttpGet("stock/{lotSerial}")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IReadOnlyList<LotStockDto>> LotStock(string lotSerial, CancellationToken ct) =>
        await Mediator.Send(new GetLotStockQuery(lotSerial), ct);

    /// <summary>Full balance sheet as CSV, streamed row by row from the database cursor.</summary>
    [HttpGet("export.csv")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public Task Export(CancellationToken ct) =>
        CsvResults.WriteAsync(Response, "inventory.csv",
            ["Lot", "Supplier", "Arrival", "Item", "Grade", "In", "Container", "Small", "SKU", "In sacks", "On hand", "At washer", "Wash in", "Wash out"],
            Mediator.CreateStream(new ExportInventoryQuery(), ct),
            r => [r.LotSerial, r.SupplierName, r.ArrivalDate, r.ItemName, r.Grade, r.QuantityIn, r.DispatchedContainer, r.DispatchedSmall, r.DispatchedSku, r.InSacks, r.OnHand,
                r.AtWash, r.WashIn, r.WashLost],
            ct);
}

[Route("api/v{version:apiVersion}/dispatches")]
public sealed class DispatchesController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<CursorPage<DispatchDto>> List([FromQuery] DispatchType? type, [FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetDispatchesQuery(type, cursor, size), ct);

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetDispatchQuery(id), ct));

    /// <summary>Creates a container (VEWC) or small (VEWO) dispatch. Stock is taken atomically —
    /// two devices dispatching the same pieces can't both succeed.</summary>
    [HttpPost]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Create([FromBody] SaveDispatchCommand command, CancellationToken ct) =>
        ToCreated(await Mediator.Send(command with { Id = null }, ct));

    [HttpPut("{id:guid}")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveDispatchCommand command, CancellationToken ct) =>
        ToResponse(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteDispatchCommand(id), ct));
}

[Route("api/v{version:apiVersion}/returns")]
public sealed class ReturnsController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>Rejected pieces still at the warehouse, grouped by supplier, waiting to go back.</summary>
    [HttpGet("pending")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IReadOnlyList<PendingRejectionGroup>> Pending(CancellationToken ct) =>
        await Mediator.Send(new GetPendingRejectionsQuery(), ct);

    [HttpGet]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<CursorPage<RejectionReturnDto>> List([FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetRejectionReturnsQuery(cursor, size), ct);

    [HttpPost]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Create([FromBody] CreateRejectionReturnCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command, ct));
}
