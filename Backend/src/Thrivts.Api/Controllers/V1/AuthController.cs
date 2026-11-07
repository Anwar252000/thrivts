using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Thrivts.Api.Extensions;
using Thrivts.Application.Features.Auth;
using Thrivts.Domain.Identity;

namespace Thrivts.Api.Controllers.V1;

[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController(IMediator mediator) : ApiControllerBase(mediator)
{
    /// <summary>The "who are you?" picker for one app (staff or owner).</summary>
    [HttpGet("users")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
    public async Task<IReadOnlyList<LoginUserCard>> Users([FromQuery] AppKind app, CancellationToken ct) =>
        await Mediator.Send(new GetLoginUsersQuery(app), ct);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new LoginCommand(request.UserId, request.Pin, request.App, UserAgent), ct));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct) =>
        ToResponse(await Mediator.Send(new RefreshSessionCommand(request.RefreshToken, UserAgent), ct));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request, CancellationToken ct)
    {
        await Mediator.Send(new LogoutCommand(request.RefreshToken), ct);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) => ToResponse(await Mediator.Send(new GetMeQuery(), ct));

    [HttpPost("change-pin")]
    [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
    public async Task<IActionResult> ChangePin([FromBody] ChangeMyPinCommand command, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(command, ct));

    private string? UserAgent => Request.Headers[HeaderNames.UserAgent].ToString();
}

public sealed record LoginRequest(Guid UserId, string Pin, AppKind App);

public sealed record RefreshRequest(string RefreshToken);
