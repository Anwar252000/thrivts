using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Deals;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/deals")]
public class AdminDealsController : AdminControllerBase
{
    public AdminDealsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DealStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetDealsQuery(status, page, pageSize), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("{dealId:guid}")]
    public async Task<IActionResult> GetById(Guid dealId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDealByIdQuery(dealId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("from-match")]
    public async Task<IActionResult> CreateFromMatch([FromBody] CreateDealFromMatchRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new CreateDealFromMatchCommand(request.RequirementId, request.SellerId, request.FinalQuantityPcs,
                request.BuyerPricePerPcUsd, request.SellerCostPerPcUsd, request.ShippingCostUsd,
                request.SourceResponseId, request.SourceOfferId, request.EstimatedDispatchDate, request.AdminNotes),
            cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(GetById), new { dealId = id }, new { id }));
    }

    [HttpPost("{dealId:guid}/advance-status")]
    public async Task<IActionResult> AdvanceStatus(Guid dealId, [FromBody] AdvanceDealStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AdvanceDealStatusCommand(dealId, request.NewStatus), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{dealId:guid}/payment")]
    public async Task<IActionResult> RecordPayment(Guid dealId, [FromBody] RecordDealPaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RecordDealPaymentCommand(dealId, request.PaymentMethod, request.PaymentReference), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{dealId:guid}/shipping")]
    public async Task<IActionResult> SetShipping(Guid dealId, [FromBody] SetDealShippingRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new SetDealShippingCommand(dealId, request.ContainerNumber, request.ContainerSize, request.ShippingLine, request.VesselName, request.BillOfLading),
            cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{dealId:guid}/tracking")]
    public async Task<IActionResult> SetTracking(Guid dealId, [FromBody] SetDealTrackingRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetDealTrackingCommand(dealId, request.TrackingNumber, request.TrackingUrl, request.Courier), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{dealId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid dealId, [FromBody] CancelDealRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CancelDealCommand(dealId, request.Reason), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpGet("{dealId:guid}/allocations")]
    public async Task<IActionResult> GetAllocations(Guid dealId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetDealAllocationsQuery(dealId), cancellationToken);
        return ToResponse(result);
    }
}

public record AdvanceDealStatusRequest(DealStatus NewStatus);

public record CreateDealFromMatchRequest(
    Guid RequirementId, Guid SellerId, int FinalQuantityPcs, decimal BuyerPricePerPcUsd, decimal SellerCostPerPcUsd,
    decimal ShippingCostUsd, Guid? SourceResponseId, Guid? SourceOfferId, DateOnly? EstimatedDispatchDate, string? AdminNotes);

public record RecordDealPaymentRequest(string PaymentMethod, string? PaymentReference);
public record SetDealShippingRequest(string? ContainerNumber, string? ContainerSize, string? ShippingLine, string? VesselName, string? BillOfLading);
public record SetDealTrackingRequest(string? TrackingNumber, string? TrackingUrl, string? Courier);
public record CancelDealRequest(string Reason);
