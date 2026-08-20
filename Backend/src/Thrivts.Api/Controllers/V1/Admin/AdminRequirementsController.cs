using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Requirements;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/requirements")]
public class AdminRequirementsController : AdminControllerBase
{
    public AdminRequirementsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] RequirementStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetRequirementsQuery(status, page, pageSize), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("bid-board")]
    public async Task<IActionResult> GetBidBoard([FromQuery] Guid? requirementId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAdminBidBoardQuery(requirementId), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("{requirementId:guid}")]
    public async Task<IActionResult> GetById(Guid requirementId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetRequirementByIdQuery(requirementId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPut("{requirementId:guid}")]
    public async Task<IActionResult> Update(Guid requirementId, [FromBody] UpdateRequirementRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UpdateRequirementCommand(requirementId, request.ItemName, request.QuantityPcs, request.Grade,
                request.DestinationCountry, request.BuyerTargetPriceUsd, request.AdminNotes),
            cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpDelete("{requirementId:guid}")]
    public async Task<IActionResult> Delete(Guid requirementId, [FromQuery] bool confirm = false, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new DeleteRequirementCommand(requirementId, confirm), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{requirementId:guid}/post-live")]
    public async Task<IActionResult> PostLive(Guid requirementId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new PostRequirementLiveCommand(requirementId), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{requirementId:guid}/public-display")]
    public async Task<IActionResult> SetPublicDisplay(Guid requirementId, [FromBody] ToggleRequirementPublicRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ToggleRequirementPublicCommand(requirementId, request.Public), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{requirementId:guid}/matching-filters")]
    public async Task<IActionResult> SetMatchingFilters(Guid requirementId, [FromBody] SetRequirementFiltersRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetRequirementFiltersCommand(requirementId, request.MinSellerTier, request.RestrictedToTags), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record UpdateRequirementRequest(
    string ItemName, int QuantityPcs, GradeType Grade, string DestinationCountry, decimal BuyerTargetPriceUsd, string? AdminNotes);
public record ToggleRequirementPublicRequest(bool Public);
public record SetRequirementFiltersRequest(SellerTier MinSellerTier, string[]? RestrictedToTags);
