using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Thrivts.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sellers")]
[Authorize(Policy = "SellerOnly")]
public class SellersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SellersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Add endpoints here as Seller commands/queries land in Thrivts.Application/Sellers —
    // e.g. POST /requirements/{id}/quotes -> SubmitQuoteCommand. Response DTOs must never
    // include buyer identity (SellerDealDto, not the shared Deal entity).
}
