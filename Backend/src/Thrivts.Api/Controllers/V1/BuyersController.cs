using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Buyers;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/buyers")]
[Authorize(Policy = "BuyerOnly")]
public class BuyersController : ApiControllerBase
{
    public BuyersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyBuyerProfileQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetBuyerDashboardQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("requirements")]
    public async Task<IActionResult> GetRequirements(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyRequirementsQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("requirements")]
    public async Task<IActionResult> PostRequirement([FromBody] PostRequirementRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new PostRequirementCommand(
            request.ItemName, request.CategoryId, request.Grade, request.QuantityPcs, request.ShippingMode,
            request.DeliveryTimelineDays, request.DestinationCountry, request.Currency, request.PricePerPc, request.Notes),
            cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(GetRequirements), new { }, new { id }));
    }

    [HttpDelete("requirements/{requirementId:guid}")]
    public async Task<IActionResult> DeleteRequirement(Guid requirementId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteMyRequirementCommand(requirementId), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpGet("requirements/{requirementId:guid}/bids")]
    public async Task<IActionResult> GetBids(Guid requirementId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetBuyerVisibleBidsQuery(requirementId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("bids/{bidId:guid}/counter")]
    public async Task<IActionResult> CounterBid(Guid bidId, [FromBody] CounterBidRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new BuyerCounterBidCommand(bidId, request.CounterBuyerPriceUsd, request.Note), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("bids/{bidId:guid}/accept")]
    public async Task<IActionResult> AcceptBid(Guid bidId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new BuyerAcceptBidCommand(bidId), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("deals")]
    public async Task<IActionResult> GetDeals(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyDealsQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("deals/{dealId:guid}/disputes")]
    public async Task<IActionResult> RaiseDispute(Guid dealId, [FromBody] RaiseDisputeRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RaiseDisputeCommand(dealId, request.Description, request.Category, request.RequestedResolution), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("message-threads")]
    public async Task<IActionResult> GetMessageThreads(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetMyMessageThreadsQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("referral-code")]
    public async Task<IActionResult> ApplyReferralCode([FromBody] ApplyReferralCodeRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ApplyReferralCodeCommand(request.Code), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record PostRequirementRequest(
    string ItemName, int CategoryId, GradeType Grade, int QuantityPcs, string? ShippingMode,
    int? DeliveryTimelineDays, string DestinationCountry, CurrencyType Currency, decimal PricePerPc, string? Notes);
public record CounterBidRequest(decimal CounterBuyerPriceUsd, string? Note);
public record RaiseDisputeRequest(string Description, string? Category, string? RequestedResolution);
public record ApplyReferralCodeRequest(string Code);
