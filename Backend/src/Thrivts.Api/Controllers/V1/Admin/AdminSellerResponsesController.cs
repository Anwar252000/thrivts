using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.SellerResponses;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/seller-responses")]
public class AdminSellerResponsesController : AdminControllerBase
{
    public AdminSellerResponsesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("{sellerResponseId:guid}/accept")]
    public async Task<IActionResult> Accept(Guid sellerResponseId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AdminAcceptSellerResponseCommand(sellerResponseId), cancellationToken);
        return ToResponse(result, dealId => Ok(new { dealId }));
    }
}
