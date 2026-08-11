using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Agencies;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/agencies")]
public class AdminAgenciesController : AdminControllerBase
{
    public AdminAgenciesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAgenciesQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("{agencyId:guid}")]
    public async Task<IActionResult> GetById(Guid agencyId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetAgencyByIdQuery(agencyId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgencyRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new CreateAgencyCommand(request.UserId, request.Email, request.AgencyName, request.OwnerFullName, request.Country,
                request.City, request.Phone, request.WhatsApp, request.CommissionRate, request.TeamSize, request.Notes),
            cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Get), new { }, new { id }));
    }

    [HttpPut("{agencyId:guid}")]
    public async Task<IActionResult> Update(Guid agencyId, [FromBody] UpdateAgencyRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateAgencyCommand(agencyId, request.CommissionRate, request.IsActive), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{agencyId:guid}/approval")]
    public async Task<IActionResult> SetApprovalStatus(Guid agencyId, [FromBody] SetApprovalStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetAgencyApprovalStatusCommand(agencyId, request.Action, request.Reason), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record CreateAgencyRequest(
    Guid UserId, string Email, string AgencyName, string OwnerFullName, string Country,
    string? City, string? Phone, string? WhatsApp, decimal CommissionRate, int? TeamSize, string? Notes);

public record UpdateAgencyRequest(decimal? CommissionRate, bool? IsActive);
