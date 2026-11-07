using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Thrivts.Api.Extensions;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Lots;
using Thrivts.Application.Features.Owner;
using Thrivts.Application.Features.Portal;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Lots;

namespace Thrivts.Api.Controllers.V1.Owner;

/// <summary>
/// Everything under /owner carries prices, money or client values, so the whole route tree is
/// gated by OwnerAccess at the class level — no staff role holds it. Individual actions add
/// stricter policies (UsersManage) where only the owner themself may act.
/// </summary>
[Route("api/v{version:apiVersion}/owner")]
[Authorize(Policy = Permissions.OwnerAccess)]
public sealed class OwnerController(IMediator mediator) : ApiControllerBase(mediator)
{
    // ---- Floor & catalogue -------------------------------------------------------------

    /// <summary>Every lot on the floor by stage, plus recently finalized ones.</summary>
    [HttpGet("floor")]
    public async Task<FloorDto> Floor([FromQuery] int finalizedLimit = 60, CancellationToken ct = default) =>
        await Mediator.Send(new GetFloorQuery(Math.Clamp(finalizedLimit, 0, 200)), ct);

    [HttpGet("items")]
    public async Task<IReadOnlyList<OwnerItemDto>> Items([FromQuery] bool includeArchived, CancellationToken ct) =>
        await Mediator.Send(new GetOwnerItemsQuery(includeArchived), ct);

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] SaveOwnerItemCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command with { Id = null }, ct));

    [HttpPut("items/{id:guid}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] SaveOwnerItemCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { Id = id }, ct));

    [HttpPut("items/{id:guid}/archived")]
    public async Task<IActionResult> SetArchived(Guid id, [FromBody] OwnerFlagRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetItemArchivedCommand(id, request.Value), ct));

    [HttpGet("item-requests")]
    public async Task<IReadOnlyList<OwnerItemRequestDto>> ItemRequests([FromQuery] ItemRequestStatus? status, CancellationToken ct) =>
        await Mediator.Send(new GetItemRequestsQuery(status), ct);

    [HttpPost("item-requests/{id:guid}/decision")]
    public async Task<IActionResult> DecideItemRequest(Guid id, [FromBody] DecideItemRequestCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { Id = id }, ct));

    /// <summary>The owner may remove a lot even after it was finalized (stock is reversed by the handler).</summary>
    [HttpDelete("lots/{id:guid}")]
    public async Task<IActionResult> DeleteLot(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteLotCommand(id, AllowFinalized: true), ct));

    /// <summary>Corrects a finalized lot line (miscount found after co-sign). Stock moves with it;
    /// blocked once the lot is invoiced. A reason is required and kept in the history.</summary>
    [HttpPut("lots/{id:guid}/items/{lineId:guid}")]
    [Idempotent]
    public async Task<IActionResult> CorrectLotItem(Guid id, Guid lineId, [FromBody] CorrectLotItemRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new CorrectFinalizedLotItemCommand(id, lineId, request.QtyA, request.QtyC, request.Rejected, request.RejectionReason, request.Reason), ct));

    /// <summary>Removes an item booked into a finalized lot by mistake (only while its stock is untouched).</summary>
    [HttpPost("lots/{id:guid}/items/{lineId:guid}/remove")]
    public async Task<IActionResult> RemoveLotItem(Guid id, Guid lineId, [FromBody] ReasonRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new RemoveFinalizedLotItemCommand(id, lineId, request.Reason), ct));

    // ---- Client portal requests ----------------------------------------------------------

    [HttpGet("client-messages")]
    public async Task<IReadOnlyList<ClientMessageDto>> ClientMessages([FromQuery] bool openOnly, CancellationToken ct) =>
        await Mediator.Send(new GetClientMessagesQuery(openOnly), ct);

    [HttpPut("client-messages/{id:guid}/done")]
    public async Task<IActionResult> SetClientMessageDone(Guid id, [FromBody] OwnerFlagRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetClientMessageDoneCommand(id, request.Value), ct));

    // ---- Client orders (owner fields) --------------------------------------------------

    [HttpGet("orders")]
    public async Task<IReadOnlyList<OwnerOrderDto>> Orders([FromQuery] bool completed, CancellationToken ct) =>
        await Mediator.Send(new GetOwnerOrdersQuery(completed), ct);

    [HttpPut("orders/{id:guid}/owner-fields")]
    public async Task<IActionResult> SetOrderOwnerFields(Guid id, [FromBody] SetOrderOwnerFieldsCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { Id = id }, ct));

    [HttpPut("orders/{id:guid}/lines/{lineId:guid}/client-ready")]
    public async Task<IActionResult> SetLineClientReady(Guid id, Guid lineId, [FromBody] ClientReadyRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetLineClientReadyCommand(id, lineId, request.ClientReady), ct));

    [HttpPost("orders/{id:guid}/payments")]
    [Idempotent]
    public async Task<IActionResult> AddOrderPayment(Guid id, [FromBody] SaveOrderPaymentCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command with { OrderId = id, PaymentId = null }, ct));

    [HttpPut("orders/{id:guid}/payments/{paymentId:guid}")]
    public async Task<IActionResult> UpdateOrderPayment(Guid id, Guid paymentId, [FromBody] SaveOrderPaymentCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { OrderId = id, PaymentId = paymentId }, ct));

    [HttpDelete("orders/{id:guid}/payments/{paymentId:guid}")]
    public async Task<IActionResult> DeleteOrderPayment(Guid id, Guid paymentId, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteOrderPaymentCommand(id, paymentId), ct));

    // ---- Approvals & goals ---------------------------------------------------------------

    [HttpGet("approvals")]
    public async Task<ApprovalsAndGoalsDto> ApprovalsAndGoals(CancellationToken ct) =>
        await Mediator.Send(new GetApprovalsAndGoalsQuery(), ct);

    [HttpPost("approvals/{id:guid}/decision")]
    public async Task<IActionResult> DecideApproval(Guid id, [FromBody] ApprovalDecisionRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DecideApprovalCommand(id, request.Approve), ct));

    [HttpPost("goals")]
    public async Task<IActionResult> CreateGoal([FromBody] SaveGoalCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command with { Id = null }, ct));

    [HttpPut("goals/{id:guid}")]
    public async Task<IActionResult> UpdateGoal(Guid id, [FromBody] SaveGoalCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { Id = id }, ct));

    [HttpDelete("goals/{id:guid}")]
    public async Task<IActionResult> DeleteGoal(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteGoalCommand(id), ct));

    // ---- Users & audit (the owner only) ------------------------------------------------

    [HttpGet("users")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IReadOnlyList<UserDto>> Users(CancellationToken ct) =>
        await Mediator.Send(new GetUsersQuery(), ct);

    [HttpPost("users")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> CreateUser([FromBody] SaveUserCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command with { Id = null }, ct));

    [HttpPut("users/{id:guid}")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] SaveUserCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { Id = id }, ct));

    [HttpPut("users/{id:guid}/active")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> SetUserActive(Guid id, [FromBody] OwnerFlagRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetUserActiveCommand(id, request.Value), ct));

    /// <summary>Field-level change history — includes price changes, so owner only.</summary>
    [HttpGet("audit")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<CursorPage<AuditEntryDto>> Audit([FromQuery] string? entityType, [FromQuery] Guid? entityId, [FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetAuditLogQuery(entityType, entityId, cursor, size), ct);

    // ---- VH Assistant ------------------------------------------------------------------

    /// <summary>Asks the assistant about the business. The server builds the live context and holds
    /// the API key; the browser only sends the conversation.</summary>
    [HttpPost("assistant")]
    [EnableRateLimiting(RateLimitingExtensions.AssistantPolicy)]
    public async Task<IActionResult> Ask([FromBody] AskAssistantCommand command, CancellationToken ct) =>
        ToResponse(await Mediator.Send(command, ct));
}

[Route("api/v{version:apiVersion}/owner/finance")]
[Authorize(Policy = Permissions.OwnerAccess)]
public sealed class OwnerFinanceController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>Hisab: every supplier's billed, paid and due.</summary>
    [HttpGet("hisab")]
    public async Task<HisabDto> Hisab([FromQuery] string? search, CancellationToken ct) =>
        await Mediator.Send(new GetHisabQuery(search), ct);

    [HttpGet("suppliers/{id:guid}")]
    public async Task<IActionResult> SupplierAccount(Guid id, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetSupplierAccountQuery(id), ct));

    [HttpPost("suppliers")]
    public async Task<IActionResult> CreateSupplier([FromBody] SaveSupplierCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command with { Id = null }, ct));

    [HttpPut("suppliers/{id:guid}")]
    public async Task<IActionResult> UpdateSupplier(Guid id, [FromBody] SaveSupplierCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command with { Id = id }, ct));

    /// <summary>Invoice form pre-filled from the lot's graded lines and the current buy prices.</summary>
    [HttpGet("lots/{lotId:guid}/invoice-draft")]
    public async Task<IActionResult> InvoiceDraft(Guid lotId, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetInvoiceDraftQuery(lotId), ct));

    [HttpPost("invoices")]
    [Idempotent]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command, ct));

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> Invoice(Guid id, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new GetInvoiceQuery(id), ct));

    [HttpDelete("invoices/{id:guid}")]
    public async Task<IActionResult> DeleteInvoice(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteInvoiceCommand(id), ct));

    [HttpPost("payments")]
    [Idempotent]
    public async Task<IActionResult> RecordPayment([FromBody] RecordVendorPaymentCommand command, CancellationToken ct) =>
        ToCreatedId(await Mediator.Send(command, ct));

    [HttpDelete("payments/{id:guid}")]
    public async Task<IActionResult> DeletePayment(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new DeleteVendorPaymentCommand(id), ct));

    [HttpGet("dues")]
    public async Task<DuesDto> Dues(CancellationToken ct) =>
        await Mediator.Send(new GetDuesQuery(), ct);

    /// <summary>Inventory on hand valued at sell prices.</summary>
    [HttpGet("valuation")]
    public async Task<ValuationDto> Valuation([FromQuery] string? search, CancellationToken ct) =>
        await Mediator.Send(new GetValuationQuery(search), ct);
}

public sealed record OwnerFlagRequest(bool Value);

public sealed record CorrectLotItemRequest(int QtyA, int QtyC, int Rejected, RejectionReason? RejectionReason, string Reason);

public sealed record ReasonRequest(string Reason);

public sealed record ClientReadyRequest(int ClientReady);

public sealed record ApprovalDecisionRequest(bool Approve);
