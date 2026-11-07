using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Communication;
using Thrivts.Application.Features.Files;
using Thrivts.Domain.Communication;

namespace Thrivts.Api.Controllers.V1.Staff;

/// <summary>Staff raise escalations to the owner and read the replies; the owner answers from the
/// owner app (same resource, owner-only verbs).</summary>
[Route("api/v{version:apiVersion}/escalations")]
public sealed class EscalationsController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpPost]
    [Idempotent]
    [Authorize(Policy = Permissions.EscalationsCreate)]
    public async Task<IActionResult> Raise([FromBody] RaiseEscalationCommand command, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(command, ct));

    [HttpGet("mine")]
    [Authorize(Policy = Permissions.EscalationsCreate)]
    public async Task<IReadOnlyList<EscalationDto>> Mine(CancellationToken ct) =>
        await Mediator.Send(new GetMyEscalationsQuery(), ct);

    [HttpPost("mine/read")]
    [Authorize(Policy = Permissions.EscalationsCreate)]
    public async Task<IActionResult> MarkRead(CancellationToken ct)
    {
        await Mediator.Send(new MarkEscalationRepliesReadCommand(), ct);
        return NoContent();
    }

    [HttpGet]
    [Authorize(Policy = Permissions.OwnerAccess)]
    public async Task<CursorPage<EscalationDto>> List([FromQuery] bool openOnly, [FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetEscalationsQuery(openOnly, cursor, size), ct);

    [HttpPost("{id:guid}/reply")]
    [Authorize(Policy = Permissions.OwnerAccess)]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new ReplyEscalationCommand(id, request.Reply, request.Resolve), ct));

    [HttpPut("{id:guid}/resolved")]
    [Authorize(Policy = Permissions.OwnerAccess)]
    public async Task<IActionResult> SetResolved(Guid id, [FromBody] SetFlagRequest request, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new SetEscalationResolvedCommand(id, request.Value), ct));
}

public sealed record ReplyRequest(string Reply, bool Resolve);

[Route("api/v{version:apiVersion}/activity")]
public sealed class ActivityController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet]
    [Authorize(Policy = Permissions.AnalyticsRead)]
    public async Task<CursorPage<ActivityDto>> List([FromQuery] ActivityArea? area, [FromQuery] string? search, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to,
        [FromQuery] string? cursor, [FromQuery] int? size, CancellationToken ct) =>
        await Mediator.Send(new GetActivityQuery(area, search, from, to, cursor, size), ct);
}

[Route("api/v{version:apiVersion}/files")]
public sealed class FilesController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>Signature images. They never change once written, so the browser may cache them
    /// for good (private: only this user's cache, never a shared proxy).</summary>
    [HttpGet("signatures/{id:guid}")]
    [Authorize(Policy = Permissions.LotsRead)]
    public async Task<IActionResult> Signature(Guid id, CancellationToken ct)
    {
        var etag = $"\"{id:N}\"";
        if (Request.Headers.IfNoneMatch.ToString() == etag)
            return StatusCode(StatusCodes.Status304NotModified);

        var result = await Mediator.Send(new GetSignatureQuery(id), ct);
        if (result.IsError)
            return ToResponse(result);

        Response.Headers.CacheControl = "private, max-age=31536000, immutable";
        Response.Headers.ETag = etag;
        return File(result.Value.Content, result.Value.ContentType);
    }
}
