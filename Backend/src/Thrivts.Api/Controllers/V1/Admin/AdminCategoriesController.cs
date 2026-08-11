using Mediator;
using Microsoft.AspNetCore.Mvc;
using Thrivts.Application.Admin.Categories;

namespace Thrivts.Api.Controllers.V1.Admin;

[Route("api/v{version:apiVersion}/admin/categories")]
public class AdminCategoriesController : AdminControllerBase
{
    public AdminCategoriesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return ToResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateCategoryCommand(request.Name, request.NameFr, request.WeightPerPieceKg, request.DisplayOrder), cancellationToken);
        return ToResponse(result, id => CreatedAtAction(nameof(Get), new { }, new { id }));
    }

    [HttpPut("{categoryId:int}")]
    public async Task<IActionResult> Update(int categoryId, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateCategoryCommand(categoryId, request.Name, request.NameFr, request.IsActive), cancellationToken);
        return ToNoContentResponse(result);
    }
}

public record CreateCategoryRequest(string Name, string? NameFr, decimal? WeightPerPieceKg, int DisplayOrder);
public record UpdateCategoryRequest(string Name, string? NameFr, bool IsActive);
