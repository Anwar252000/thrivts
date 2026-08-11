using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Admin.Categories;

public sealed record GetCategoriesQuery : IQuery<ErrorOr<List<CategoryDto>>>;

public sealed record CategoryDto(int Id, string Name, string? NameFr, decimal? WeightPerPieceKg, int DisplayOrder, bool IsActive);

public sealed class GetCategoriesQueryHandler : IQueryHandler<GetCategoriesQuery, ErrorOr<List<CategoryDto>>>
{
    private readonly IApplicationDbContext _db;

    public GetCategoriesQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async ValueTask<ErrorOr<List<CategoryDto>>> Handle(GetCategoriesQuery query, CancellationToken cancellationToken)
    {
        // Categories power buyer-facing requirement forms too — read access is not admin-gated.
        var result = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(c.Id, c.Name, c.NameFr, c.WeightPerPieceKg, c.DisplayOrder, c.IsActive))
            .ToListAsync(cancellationToken);

        return result;
    }
}
