using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Thrivts.Api.Extensions;

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

    // Add endpoints here as Public commands/queries land in Thrivts.Application/Public —
    // e.g. GET /activity -> GetPublicActivityQuery, POST /partner-applications -> SubmitPartnerApplicationCommand.
}
