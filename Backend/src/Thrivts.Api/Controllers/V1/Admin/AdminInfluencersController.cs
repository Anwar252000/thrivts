using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Influencers;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/influencers")]
public class AdminInfluencersController : AdminControllerBase
{
    public AdminInfluencersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetInfluencersQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInfluencerRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new CreateInfluencerCommand(request.FullName, request.Email, request.Phone, request.Instagram,
                request.Tiktok, request.ReferralCode, request.CommissionRate, request.UserId),
            cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Get), new { }, new { id }));
    }
}

public record CreateInfluencerRequest(
    string FullName, string Email, string? Phone, string? Instagram, string? Tiktok,
    string? ReferralCode, decimal CommissionRate, Guid? UserId);
