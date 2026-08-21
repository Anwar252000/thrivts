using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Thrivts.Api.Extensions;
using Thrivts.Application.Admin.Categories;
using Thrivts.Application.Public;

namespace Thrivts.Api.Controllers.V1;

/// <summary>
/// Anonymous-allowed endpoints (public activity feed, partner applications, referral codes).
/// Rate-limited via the "public" fixed-window policy — these are the surfaces that were open
/// to anonymous callers/bots on the old Supabase RPCs.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/public")]
[EnableRateLimiting(RateLimitingExtensions.PublicPolicy)]
public class PublicController : ControllerBase
{
    private readonly IMediator _mediator;

    public PublicController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Categories power buyer-facing requirement forms too — GetCategoriesQuery itself
    /// has no role check, only the admin controller route was gating it.</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return result.Match<IActionResult>(Ok, errors => Problem(title: errors[0].Description));
    }

    [HttpGet("exchange-rates")]
    public async Task<IActionResult> GetExchangeRates(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPublicExchangeRatesQuery(), cancellationToken);
        return result.Match<IActionResult>(Ok, errors => Problem(title: errors[0].Description));
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetActivity([FromQuery] int limit = 20, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPublicActivityQuery(limit), cancellationToken);
        return result.Match<IActionResult>(Ok, errors => Problem(title: errors[0].Description));
    }

    [HttpGet("agencies/{code}")]
    public async Task<IActionResult> GetAgencyName(string code, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAgencyPublicNameQuery(code), cancellationToken);
        return result.Match<IActionResult>(
            name => name is null ? NotFound() : Ok(new { agencyName = name }),
            errors => Problem(title: errors[0].Description));
    }
}
