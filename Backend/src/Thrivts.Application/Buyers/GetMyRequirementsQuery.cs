using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Powers the buyer portal's "My requirements" list — mirrors buyer.html's loadMyRequirements().</summary>
public sealed record GetMyRequirementsQuery : IQuery<ErrorOr<List<MyRequirementListItemDto>>>;

public sealed record MyRequirementListItemDto(
    Guid Id, string RequirementNumber, string ItemName, string? CategoryName, int QuantityPcs, GradeType Grade,
    string DestinationCountry, decimal TargetPricePerPc, CurrencyType Currency, RequirementStatus Status, DateTimeOffset CreatedAt);

public sealed class GetMyRequirementsQueryHandler : IQueryHandler<GetMyRequirementsQuery, ErrorOr<List<MyRequirementListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyRequirementsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<MyRequirementListItemDto>>> Handle(GetMyRequirementsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var result = await (
            from r in _db.Requirements.AsNoTracking()
            where r.BuyerId == _currentUser.UserId
            join c in _db.Categories.AsNoTracking() on r.CategoryId equals c.Id into categoryJoin
            from category in categoryJoin.DefaultIfEmpty()
            orderby r.CreatedAt descending
            select new MyRequirementListItemDto(
                r.Id, r.RequirementNumber, r.ItemName, category != null ? category.Name : null, r.QuantityPcs, r.Grade,
                r.DestinationCountry, r.BuyerTargetPriceOriginal ?? r.BuyerTargetPriceUsd, r.BuyerCurrency, r.Status, r.CreatedAt))
            .Take(300)
            .ToListAsync(cancellationToken);

        return result;
    }
}
