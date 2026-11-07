using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Api.Filters;
using Thrivts.Application.Common.Security;
using Thrivts.Application.Features.Catalog;

namespace Thrivts.Api.Controllers.V1.Staff;

/// <summary>Reference data shared by both apps. Nothing here carries a price — the staff-safe
/// catalogue DTO has no price fields at all; prices live only under /owner.</summary>
[Route("api/v{version:apiVersion}/catalog")]
public sealed class CatalogController(IMediator mediator) : ApiControllerBase(mediator)
{
    [HttpGet("items")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public async Task<IReadOnlyList<CatalogItemDto>> Items(CancellationToken ct) =>
        await Mediator.Send(new GetCatalogItemsQuery(), ct);

    [HttpGet("suppliers")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public async Task<IReadOnlyList<SupplierDto>> Suppliers(CancellationToken ct) =>
        await Mediator.Send(new GetSuppliersQuery(), ct);

    /// <summary>Idempotent by name: an existing supplier (any casing) comes back with 200, a new one with 201.</summary>
    [HttpPost("suppliers")]
    [Authorize(Policy = Permissions.SuppliersCreate)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierCommand command, CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return result.AlreadyExisted ? Ok(result.Supplier) : StatusCode(StatusCodes.Status201Created, result.Supplier);
    }

    [HttpGet("sku-categories")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public async Task<IReadOnlyList<SkuCategoryDto>> SkuCategories(CancellationToken ct) =>
        await Mediator.Send(new GetSkuCategoriesQuery(), ct);

    [HttpPut("sku-categories")]
    [Authorize(Policy = Permissions.InventoryManage)]
    public async Task<SkuCategoryDto> UpsertSkuCategory([FromBody] UpsertSkuCategoryCommand command, CancellationToken ct) =>
        await Mediator.Send(command, ct);

    [HttpGet("labourers")]
    [Authorize(Policy = Permissions.CatalogRead)]
    public async Task<IReadOnlyList<LabourerDto>> Labourers(CancellationToken ct) =>
        await Mediator.Send(new GetLabourersQuery(), ct);

    [HttpPost("labourers")]
    [Authorize(Policy = Permissions.AttendanceManage)]
    public async Task<IActionResult> AddLabourer([FromBody] AddLabourerCommand command, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await Mediator.Send(command, ct));

    [HttpDelete("labourers/{id:guid}")]
    [Authorize(Policy = Permissions.AttendanceManage)]
    public async Task<IActionResult> RemoveLabourer(Guid id, CancellationToken ct) =>
        ToNoContent(await Mediator.Send(new RemoveLabourerCommand(id), ct));

    /// <summary>Staff ask the owner to add a missing item; the owner prices and approves it.</summary>
    [HttpPost("item-requests")]
    [Idempotent]
    [Authorize(Policy = Permissions.ItemRequestsCreate)]
    public async Task<IActionResult> RequestItem([FromBody] RequestItemCommand command, CancellationToken ct) =>
        ToCreated(await Mediator.Send(command, ct));

    [HttpGet("item-requests/mine")]
    [Authorize(Policy = Permissions.ItemRequestsCreate)]
    public async Task<IReadOnlyList<ItemRequestDto>> MyItemRequests(CancellationToken ct) =>
        await Mediator.Send(new GetMyItemRequestsQuery(), ct);
}
