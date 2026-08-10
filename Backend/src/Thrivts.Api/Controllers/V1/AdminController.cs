using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Buyers;
using Thrivts.Application.Admin.Deals;
using Thrivts.Application.Admin.Sellers;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("deals/{dealId:guid}/advance-status")]
    public async Task<IActionResult> AdvanceDealStatus(Guid dealId, [FromBody] AdvanceDealStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new AdvanceDealStatusCommand(dealId, request.NewStatus), cancellationToken);

        return result.Match<IActionResult>(
            _ => NoContent(),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    [HttpPost("sellers/{sellerId:guid}/approval")]
    public async Task<IActionResult> SetSellerApprovalStatus(Guid sellerId, [FromBody] SetApprovalStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetSellerApprovalStatusCommand(sellerId, request.Action, request.Reason), cancellationToken);

        return result.Match<IActionResult>(
            _ => NoContent(),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    [HttpPost("sellers/{sellerId:guid}/kyc")]
    public async Task<IActionResult> SetSellerKycVerification(Guid sellerId, [FromBody] SetKycVerificationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetSellerKycVerificationCommand(sellerId, request.Verified), cancellationToken);

        return result.Match<IActionResult>(
            _ => NoContent(),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    [HttpPost("buyers/{buyerId:guid}/approval")]
    public async Task<IActionResult> SetBuyerApprovalStatus(Guid buyerId, [FromBody] SetApprovalStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SetBuyerApprovalStatusCommand(buyerId, request.Action, request.Reason), cancellationToken);

        return result.Match<IActionResult>(
            _ => NoContent(),
            errors => Problem(title: errors[0].Description, statusCode: MapStatusCode(errors[0].Type)));
    }

    private static int MapStatusCode(ErrorOr.ErrorType errorType) => errorType switch
    {
        ErrorOr.ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorOr.ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorOr.ErrorType.Validation => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError
    };
}

public record AdvanceDealStatusRequest(DealStatus NewStatus);
public record SetApprovalStatusRequest(ProfileApprovalAction Action, string? Reason = null);
public record SetKycVerificationRequest(bool Verified, string? Notes = null);
