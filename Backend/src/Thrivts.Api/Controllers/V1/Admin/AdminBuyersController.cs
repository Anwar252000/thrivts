using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Buyers;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/buyers")]
public class AdminBuyersController : AdminControllerBase
{
    public AdminBuyersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetBuyersQuery(page, pageSize), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("{buyerId:guid}")]
    public async Task<IActionResult> GetById(Guid buyerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetBuyerByIdQuery(buyerId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("{buyerId:guid}/approval")]
    public async Task<IActionResult> SetApprovalStatus(Guid buyerId, [FromBody] SetApprovalStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetBuyerApprovalStatusCommand(buyerId, request.Action, request.Reason), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPut("{buyerId:guid}")]
    public async Task<IActionResult> Update(Guid buyerId, [FromBody] UpdateBuyerRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UpdateBuyerCommand(buyerId, request.CompanyName, request.Country, request.City, request.Website, request.Instagram),
            cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpDelete("{buyerId:guid}")]
    public async Task<IActionResult> Delete(Guid buyerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteBuyerCommand(buyerId), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{buyerId:guid}/premium")]
    public async Task<IActionResult> SetPremium(Guid buyerId, [FromBody] SetBuyerPremiumRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetBuyerPremiumCommand(buyerId, request.IsPremium), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record SetApprovalStatusRequest(ProfileApprovalAction Action, string? Reason = null);
public record UpdateBuyerRequest(string CompanyName, string Country, string? City, string? Website, string? Instagram);
public record SetBuyerPremiumRequest(bool IsPremium);
