using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Domain.Exceptions;
using ValidationException = Thrivts.Application.Common.Exceptions.ValidationException;

namespace Thrivts.Api.ExceptionHandlers;

/// <summary>
/// Every unhandled exception ends here and leaves as an RFC 7807 problem response carrying a
/// traceId the user can quote to support. Business-rule failures return their message (written
/// for the user); anything unexpected returns a generic 500 and is logged in full server-side —
/// stack traces, SQL and internals never reach the client.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else if (status != StatusCodes.Status499ClientClosedRequest)
            logger.LogInformation("Request rejected with {Status}: {Reason}", status, title);

        if (status == StatusCodes.Status499ClientClosedRequest || httpContext.Response.HasStarted)
            return true;

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
        };

        if (exception is ValidationException validation)
            problem.Extensions["errors"] = validation.Errors;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static (int Status, string Title, string? Detail) Map(Exception exception) => exception switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "Some fields need fixing.", null),
        DomainException e => (StatusCodes.Status422UnprocessableEntity, e.Message, null),
        ConflictException e => (StatusCodes.Status409Conflict, e.Message, null),
        DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Someone else changed this just now. Reload and try again.", null),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
            (StatusCodes.Status409Conflict, "That already exists.", null),
        DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.CheckViolation } } =>
            (StatusCodes.Status409Conflict, "That change would leave the numbers inconsistent (for example, negative stock). Reload and try again.", null),
        NotFoundException e => (StatusCodes.Status404NotFound, e.Message, null),
        ForbiddenAccessException => (StatusCodes.Status403Forbidden, "You don't have permission to do that.", null),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Please sign in again.", null),
        BadHttpRequestException e => (e.StatusCode, "The request could not be read.", null),
        OperationCanceledException => (StatusCodes.Status499ClientClosedRequest, "Request cancelled.", null),
        _ => (StatusCodes.Status500InternalServerError, "Something went wrong on our side. Please try again.", $"Quote reference {Activity.Current?.TraceId} if this keeps happening."),
    };
}
