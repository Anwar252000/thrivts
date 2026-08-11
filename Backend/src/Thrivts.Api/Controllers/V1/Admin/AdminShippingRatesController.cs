using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.ShippingRates;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/shipping-rates")]
public class AdminShippingRatesController : AdminControllerBase
{
    public AdminShippingRatesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetShippingRatesQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateShippingRateRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new CreateShippingRateCommand(request.DestinationCountry, request.RateUsdPerKg, request.FlatRateUsd, request.TransitDays),
            cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Get), new { }, new { id }));
    }

    [HttpPut("{shippingRateId:int}")]
    public async Task<IActionResult> Update(int shippingRateId, [FromBody] UpdateShippingRateRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(
            new UpdateShippingRateCommand(shippingRateId, request.RateUsdPerKg, request.FlatRateUsd, request.IsActive), cancellationToken);
        return ToNoContentResponse(result);
    }

    [HttpDelete("{shippingRateId:int}")]
    public async Task<IActionResult> Delete(int shippingRateId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteShippingRateCommand(shippingRateId), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record CreateShippingRateRequest(string DestinationCountry, decimal? RateUsdPerKg, decimal? FlatRateUsd, int? TransitDays);
public record UpdateShippingRateRequest(decimal? RateUsdPerKg, decimal? FlatRateUsd, bool IsActive);
