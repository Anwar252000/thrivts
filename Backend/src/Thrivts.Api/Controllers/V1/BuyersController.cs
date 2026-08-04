using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Thrivts.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/buyers")]
[Authorize(Policy = "BuyerOnly")]
public class BuyersController : ControllerBase
{
    private readonly IMediator _mediator;

    public BuyersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Add endpoints here as Buyer commands/queries land in Thrivts.Application/Buyers —
    // e.g. GET /deals -> GetBuyerDealsQuery scoped to currentUser.BuyerId, never a client-supplied id.
}
