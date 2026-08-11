using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

public sealed record GetRequirementsQuery(RequirementStatus? Status, int Page, int PageSize)
    : IQuery<ErrorOr<PagedResult<RequirementListItemDto>>>;

public sealed record RequirementListItemDto(
    Guid Id, string RequirementNumber, Guid BuyerId, string ItemName, int QuantityPcs, GradeType Grade,
    string DestinationCountry, RequirementStatus Status, DateTimeOffset CreatedAt);

public sealed class GetRequirementsQueryHandler : IQueryHandler<GetRequirementsQuery, ErrorOr<PagedResult<RequirementListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetRequirementsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PagedResult<RequirementListItemDto>>> Handle(GetRequirementsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list requirements.");

        var (page, pageSize) = PagedResult<RequirementListItemDto>.Normalize(query.Page, query.PageSize);

        var requirements = _db.Requirements.AsNoTracking();
        if (query.Status is not null)
            requirements = requirements.Where(r => r.Status == query.Status);

        var totalCount = await requirements.CountAsync(cancellationToken);
        var items = await requirements
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RequirementListItemDto(
                r.Id, r.RequirementNumber, r.BuyerId, r.ItemName, r.QuantityPcs, r.Grade,
                r.DestinationCountry, r.Status, r.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<RequirementListItemDto>(items, totalCount, page, pageSize);
    }
}
