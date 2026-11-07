using Asp.Versioning;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Thrivts.Api.Controllers.V1;

/// <summary>Shared base: versioning and the ErrorOr → HTTP status mapping. Every controller adds
/// its own [Authorize(Policy = Permissions.X)]; the fallback policy rejects anything unauthenticated.</summary>
[ApiController]
[ApiVersion("1.0")]
[Produces("application/json")]
public abstract class ApiControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;

    protected IActionResult ToResponse<T>(ErrorOr<T> result, Func<T, IActionResult>? onSuccess = null) =>
        result.Match(value => onSuccess?.Invoke(value) ?? Ok(value), Problem);

    protected IActionResult ToNoContent(ErrorOr<Success> result) =>
        result.Match<IActionResult>(_ => NoContent(), Problem);

    protected IActionResult ToCreated<T>(ErrorOr<T> result) =>
        result.Match(value => StatusCode(StatusCodes.Status201Created, value), Problem);

    /// <summary>201 with a <c>{ "id": ... }</c> body, so clients never have to parse a bare string.</summary>
    protected IActionResult ToCreatedId(ErrorOr<Guid> result) =>
        result.Match(id => StatusCode(StatusCodes.Status201Created, new IdResponse(id)), Problem);

    /// <summary>For updates whose handler returns the row id: success is simply 204.</summary>
    protected IActionResult ToNoContent(ErrorOr<Guid> result) =>
        result.Match<IActionResult>(_ => NoContent(), Problem);

    private IActionResult Problem(List<Error> errors)
    {
        var error = errors[0];
        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError,
        };

        return Problem(title: error.Description, statusCode: status);
    }
}

public sealed record IdResponse(Guid Id);
