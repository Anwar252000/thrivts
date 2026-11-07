using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Thrivts.Api.Extensions;
using Thrivts.Application.Features.Portal;
using Thrivts.Domain.Orders;

namespace Thrivts.Api.Controllers.V1;

/// <summary>
/// Buyers' order portals. Anonymous by design — the order's login code is the credential — so:
/// the code travels in the POST body (never a URL, where it would land in logs and history), a
/// strict per-IP rate limit makes guessing codes impractical, unknown and malformed codes get the
/// same answer, and only the narrow price-free portal view is ever returned. Portal sites live on
/// their own origins, allowed by a separate CORS policy without credentials.
/// </summary>
[Route("api/v{version:apiVersion}/portal")]
[AllowAnonymous]
[EnableCors(PortalCors.PolicyName)]
[EnableRateLimiting(RateLimitingExtensions.PortalPolicy)]
public sealed class PortalController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost("order")]
    public async Task<IActionResult> Order([FromBody] PortalCodeRequest request, CancellationToken ct)
    {
        Response.Headers.CacheControl = "no-store";
        return ToResponse(await Mediator.Send(new GetPortalOrderQuery(request.Code ?? string.Empty), ct));
    }

    [HttpPost("messages")]
    public async Task<IActionResult> Message([FromBody] PortalMessageRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SendPortalMessageCommand(request.Code ?? string.Empty, request.Type, request.Message ?? string.Empty, request.RequestedWhen), ct));
}

public static class PortalCors
{
    public const string PolicyName = "portal";
}

public sealed record PortalCodeRequest(string? Code);

public sealed record PortalMessageRequest(string? Code, ClientMessageType Type, string? Message, string? RequestedWhen);
