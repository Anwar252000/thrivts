using Asp.Versioning;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;
using Thrivts.Api.Extensions;
using Thrivts.Application.Auth;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Api.Controllers.V1;

/// <summary>
/// The browser's ONLY path to authentication — it never talks to Supabase directly. These
/// endpoints proxy to Supabase Auth via ISupabaseAuthClient (see README_HANDOVER.md / migration
/// plan §2 "Key shift": the .NET API is the single client of Supabase, for auth as well as data).
/// Rate-limited like the rest of the anonymous-reachable surface — credential guessing is exactly
/// what this protects against.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[EnableRateLimiting(RateLimitingExtensions.PublicPolicy)]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password), cancellationToken);

        return result.Match<IActionResult>(
            session => Ok(AuthResponse.From(session)),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(request.RefreshToken), cancellationToken);

        return result.Match<IActionResult>(
            session => Ok(AuthResponse.From(session)),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var accessToken = Request.Headers[HeaderNames.Authorization]
            .ToString()
            .Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);

        var result = await _mediator.Send(new LogoutCommand(accessToken), cancellationToken);

        return result.Match<IActionResult>(
            _ => NoContent(),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    /// <summary>The only place the frontend learns its own role — it never reads Supabase/profiles directly.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCurrentUserQuery(), cancellationToken);

        return result.Match<IActionResult>(
            user => Ok(CurrentUserResponse.From(user)),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    private static int MapStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}

public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);

public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, Guid UserId, string Email)
{
    public static AuthResponse From(SupabaseSession session) =>
        new(session.AccessToken, session.RefreshToken, session.ExpiresIn, session.UserId, session.Email);
}

public record CurrentUserResponse(Guid Id, string Email, string FullName, string Role, string ApprovalStatus, bool IsActive)
{
    public static CurrentUserResponse From(CurrentUserDto user) =>
        new(user.Id, user.Email, user.FullName, user.Role.ToString().ToLowerInvariant(),
            user.ApprovalStatus.ToString().ToLowerInvariant(), user.IsActive);
}
