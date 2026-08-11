using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Offers;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/offers")]
public class AdminOffersController : AdminControllerBase
{
    public AdminOffersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Guid? requirementId, [FromQuery] OfferStatus? status, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetOffersQuery(requirementId, status), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("{offerId:guid}/rounds")]
    public async Task<IActionResult> GetRounds(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetOfferRoundsQuery(offerId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOfferRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateOfferCommand(request.RequirementId, request.SellerId, request.OfferPricePerPc), cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Create), new { }, new { id }));
    }

    [HttpPost("{offerId:guid}/rounds")]
    public async Task<IActionResult> PostRound(Guid offerId, [FromBody] PostOfferRoundRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new PostOfferRoundCommand(offerId, request.Kind, request.PricePerPcUsd, request.Notes), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{offerId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AdminAcceptOfferCommand(offerId), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{offerId:guid}/decline")]
    public async Task<IActionResult> Decline(Guid offerId, [FromBody] DeclineOfferRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AdminDeclineOfferCommand(offerId, request.Reason), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpDelete("{offerId:guid}")]
    public async Task<IActionResult> Delete(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteOfferCommand(offerId), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record CreateOfferRequest(Guid RequirementId, Guid SellerId, decimal OfferPricePerPc);
public record PostOfferRoundRequest(string Kind, decimal? PricePerPcUsd, string? Notes);
public record DeclineOfferRequest(string Reason);
