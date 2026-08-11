using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.ExchangeRates;
using Thrivts.Domain.Enums;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/exchange-rates")]
public class AdminExchangeRatesController : AdminControllerBase
{
    public AdminExchangeRatesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetExchangeRatesQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Set([FromBody] SetExchangeRateRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new SetExchangeRateCommand(request.Currency, request.RateToUsd, request.Notes), cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Get), new { }, new { id }));
    }
}

public record SetExchangeRateRequest(CurrencyType Currency, decimal RateToUsd, string? Notes);
