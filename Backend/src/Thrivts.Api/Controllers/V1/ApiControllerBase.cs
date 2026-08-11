using Asp.Versioning;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Thrivts.Api.Controllers.V1;

/// <summary>Shared base for every controller — versioning and the ErrorOr-to-HTTP-status mapping
/// every endpoint uses. Does not itself require authorization; role-specific bases (e.g.
/// Admin.AdminControllerBase) add their own [Authorize] policy on top of this.</summary>
[ApiController]
[ApiVersion("1.0")]
public abstract class ApiControllerBase : ControllerBase
{
    protected readonly IMediator Mediator;

    protected ApiControllerBase(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected IActionResult ToResponse<T>(ErrorOr<T> result, Func<T, IActionResult>? onSuccess = null)
    {
        return result.Match(
            value => onSuccess?.Invoke(value) ?? Ok(value),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    protected IActionResult ToNoContentResponse(ErrorOr<Success> result) =>
        result.Match<IActionResult>(
            _ => NoContent(),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));

    private static int MapStatusCode(ErrorType errorType) => errorType switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}
