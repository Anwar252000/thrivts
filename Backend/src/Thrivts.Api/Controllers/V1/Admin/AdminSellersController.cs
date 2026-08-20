using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Sellers;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/sellers")]
public class AdminSellersController : AdminControllerBase
{
    public AdminSellersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetSellersQuery(page, pageSize), cancellationToken);
        return ToResponse(result);
    }

    [HttpGet("{sellerId:guid}")]
    public async Task<IActionResult> GetById(Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSellerByIdQuery(sellerId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("{sellerId:guid}/approval")]
    public async Task<IActionResult> SetApprovalStatus(Guid sellerId, [FromBody] SetSellerApprovalStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetSellerApprovalStatusCommand(sellerId, request.Action, request.Reason, request.Tags), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("{sellerId:guid}/kyc")]
    public async Task<IActionResult> SetKycVerification(Guid sellerId, [FromBody] SetKycVerificationRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetSellerKycVerificationCommand(sellerId, request.Verified, request.Notes), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPut("{sellerId:guid}")]
    public async Task<IActionResult> Update(Guid sellerId, [FromBody] UpdateSellerRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UpdateSellerCommand(sellerId, request.Tier, request.Tags, request.Phone, request.WhatsApp, request.ReferenceContact, request.TierNotes),
            cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpDelete("{sellerId:guid}")]
    public async Task<IActionResult> Delete(Guid sellerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteSellerCommand(sellerId), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record SetSellerApprovalStatusRequest(ProfileApprovalAction Action, string? Reason = null, string[]? Tags = null);
public record SetKycVerificationRequest(bool Verified, string? Notes = null);
public record UpdateSellerRequest(SellerTier? Tier, string[]? Tags, string? Phone, string? WhatsApp, string? ReferenceContact, string? TierNotes);
