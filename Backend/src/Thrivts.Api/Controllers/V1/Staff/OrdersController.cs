using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Orders;

namespace Thrivts.Api.Controllers.V1.Staff;

/// <summary>Client requirements as staff see them — quantities and progress only. Order value,
/// payments and the client login code are owner fields, served from /owner/orders.</summary>
[Route("api/v{version:apiVersion}/orders")]
public sealed class OrdersController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    [Authorize(Policy = Permissions.RequirementsRead)]
    public async Task<IReadOnlyList<StaffOrderDto>> List([FromQuery] bool includeCompleted, [FromQuery] string? search, CancellationToken ct) =>
        await Mediator.Send(new GetStaffOrdersQuery(includeCompleted, search), ct);

    [HttpPost]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteOrderCommand(id), ct));

    [HttpPut("{id:guid}/completed")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> SetCompleted(Guid id, [FromBody] SetFlagRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetOrderCompletedCommand(id, request.Value), ct));

    [HttpPost("{id:guid}/ready")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> MarkReady(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new MarkOrderReadyCommand(id), ct));

    [HttpPost("{id:guid}/lines")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> AddLine(Guid id, [FromBody] SaveOrderLineCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command with { OrderId = id, LineId = null }, ct));

    [HttpPut("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> UpdateLine(Guid id, Guid lineId, [FromBody] SaveOrderLineCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { OrderId = id, LineId = lineId }, ct));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new RemoveOrderLineCommand(id, lineId), ct));

    /// <summary>Bulk import from the client's spreadsheet (parsed in the browser). Re-importing the
    /// same file merges instead of duplicating.</summary>
    [HttpPost("import")]
    [Authorize(Policy = Permissions.RequirementsManage)]
    public async Task<ImportResult> Import([FromBody] ImportOrdersCommand command, CancellationToken ct) =>
        await Mediator.Send(command, ct);
}

public sealed record SetFlagRequest(bool Value);
