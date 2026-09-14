using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.PurchaseOrders;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/deals/{dealId:guid}/purchase-order")]
public class AdminPurchaseOrdersController : AdminControllerBase
{
    public AdminPurchaseOrdersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid dealId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPurchaseOrderByDealIdQuery(dealId), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost("issue")]
    public async Task<IActionResult> Issue(Guid dealId, [FromBody] IssuePurchaseOrderRequest? request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new IssuePurchaseOrderCommand(dealId, request?.DueDays ?? 3), cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Get), new { dealId }, new { id }));
    }

    [HttpPost("mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid dealId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AdminMarkPoPaidCommand(dealId), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify(Guid dealId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new VerifyPurchaseOrderCommand(dealId), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpPost("payment-link")]
    public async Task<IActionResult> AddPaymentLink(Guid dealId, [FromBody] AddPaymentLinkRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AddPaymentLinkCommand(dealId, request.Link), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record IssuePurchaseOrderRequest(int? DueDays);
public record AddPaymentLinkRequest(string Link);
