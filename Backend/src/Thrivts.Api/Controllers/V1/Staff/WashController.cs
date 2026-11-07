using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Wash;
using Thrivts.Domain.Wash;

namespace Thrivts.Api.Controllers.V1.Staff;

/// <summary>Washing: finalized stock goes to a washer (Irfan, Kashif…), comes back as A/C or
/// rejected, and the washer is paid for the good pieces.</summary>
[Route("api/v{version:apiVersion}/wash")]
public sealed class WashController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet("vendors")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IReadOnlyList<WashVendorDto>> Vendors([FromQuery] bool includeInactive, CancellationToken ct) =>
        await Mediator.Send(new GetWashVendorsQuery(includeInactive), ct);

    [HttpPost("vendors")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> CreateVendor([FromBody] CreateWashVendorCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command, ct));

    /// <summary>Saves a vendor's active flag and whole rate card (Rs per piece by category).</summary>
    [HttpPut("vendors/{id:guid}")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> UpdateVendor(Guid id, [FromBody] UpdateWashVendorRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetWashVendorCommand(id, request.IsActive, request.Rates), ct));

    [HttpGet("categories")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<WashCategoriesDto> Categories(CancellationToken ct) =>
        await Mediator.Send(new GetWashCategoriesQuery(), ct);

    [HttpPut("item-categories")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> SetItemCategory([FromBody] SetWashItemCategoryCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command, ct));

    /// <summary>"Add from lot": a finalized lot's free stock with suggested wash categories.</summary>
    [HttpGet("candidates/{lotSerial}")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IActionResult> Candidates(string lotSerial, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetWashCandidatesQuery(lotSerial), ct));

    [HttpGet("summary")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<WashSummaryDto> Summary(CancellationToken ct) =>
        await Mediator.Send(new GetWashSummaryQuery(), ct);

    [HttpGet("batches")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<CursorPage<WashBatchDto>> Batches([FromQuery] WashStatus? status, [FromQuery] string? search, [FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetWashBatchesQuery(status, search, cursor, size), ct);

    [HttpGet("batches/{id:guid}")]
    [Authorize(Policy = Permissions.InventoryRead)]
    public async Task<IActionResult> Batch(Guid id, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetWashBatchQuery(id), ct));

    [HttpPost("batches")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Send([FromBody] SendToWashCommand command, CancellationToken ct) =>
        ToCreated(await Mediator.Send(command, ct));

    /// <summary>Saves the running totals received back (A, C, rejected) per line.</summary>
    [HttpPut("batches/{id:guid}/received")]
    [Idempotent]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Receive(Guid id, [FromBody] ReceiveWashRequest request, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new ReceiveWashCommand(id, request.Lines, request.PaymentStatus), ct));

    [HttpPost("batches/{id:guid}/finalize")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Finalize(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new FinalizeWashCommand(id), ct));

    [HttpPost("batches/{id:guid}/reopen")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new ReopenWashCommand(id), ct));

    [HttpPut("batches/{id:guid}/payment")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> SetPayment(Guid id, [FromBody] WashPaymentRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetWashPaymentCommand(id, request.Status), ct));

    /// <summary>Cancels a wash logged by mistake — only while nothing has come back.</summary>
    [HttpDelete("batches/{id:guid}")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteWashCommand(id), ct));
}

public sealed record UpdateWashVendorRequest(bool IsActive, IReadOnlyList<WashRateInput> Rates);

public sealed record ReceiveWashRequest(IReadOnlyList<ReceiveWashLine> Lines, WashPaymentStatus? PaymentStatus);

public sealed record WashPaymentRequest(WashPaymentStatus Status);
