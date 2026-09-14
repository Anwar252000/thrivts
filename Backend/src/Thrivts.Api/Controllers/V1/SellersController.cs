using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.PurchaseOrders;
using Thrivts.Application.Sellers;

namespace Thrivts.Api.Controllers.V1;

/// <summary>Response DTOs here must never include buyer identity — see SellerDealDto /
/// GetSellerOffersQuery's own doc comments for the anonymity moat this mirrors from the buyer side.</summary>
[Route("api/v{version:apiVersion}/sellers")]
[Authorize(Policy = "SellerOnly")]
public class SellersController : ApiControllerBase
{
    public SellersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMySellerProfileQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSellerDashboardQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("requirements")]
    public async Task<IActionResult> GetOpenRequirements(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetOpenRequirementsForSellerQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("quotes")]
    public async Task<IActionResult> GetQuotes(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyQuotesQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("quotes")]
    public async Task<IActionResult> SubmitQuote([FromBody] SubmitQuoteRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SubmitOrReviseQuoteCommand(
            request.RequirementId, request.AvailableQuantityPcs, request.PricePerPcUsd, request.SellerNotes), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("quotes/{bidId:guid}/respond")]
    public async Task<IActionResult> RespondToBuyerCounter(Guid bidId, [FromBody] RespondToCounterRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RespondToBuyerCounterCommand(bidId, request.Action, request.NewPriceUsd, request.Note), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpGet("offers")]
    public async Task<IActionResult> GetOffers(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSellerOffersQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("offers/{offerId:guid}/respond")]
    public async Task<IActionResult> RespondToOffer(Guid offerId, [FromBody] RespondToOfferRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SellerRespondToOfferCommand(offerId, request.Action, request.PricePerPcUsd, request.Notes), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("offers/mark-viewed")]
    public async Task<IActionResult> MarkOffersViewed([FromBody] MarkOffersViewedRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new MarkOffersViewedCommand(request.OfferIds), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpGet("deals")]
    public async Task<IActionResult> GetDeals(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSellerDealsQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("deals/{dealId:guid}/purchase-order")]
    public async Task<IActionResult> GetPurchaseOrder(Guid dealId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPurchaseOrderByDealIdQuery(dealId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("deals/{dealId:guid}/mark-ready")]
    public async Task<IActionResult> MarkOrderReady(Guid dealId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new MarkOrderReadyCommand(dealId), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record SubmitQuoteRequest(Guid RequirementId, int AvailableQuantityPcs, decimal PricePerPcUsd, string? SellerNotes);
public record RespondToCounterRequest(string Action, decimal? NewPriceUsd, string? Note);
public record RespondToOfferRequest(string Action, decimal? PricePerPcUsd, string? Notes);
public record MarkOffersViewedRequest(Guid[] OfferIds);
