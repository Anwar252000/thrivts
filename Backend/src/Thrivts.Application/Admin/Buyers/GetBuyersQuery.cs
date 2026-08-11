using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

public sealed record GetBuyersQuery(int Page, int PageSize) : IQuery<ErrorOr<PagedResult<BuyerListItemDto>>>;

public sealed record BuyerListItemDto(
    Guid Id, string Email, string CompanyName, string Country, string? City,
    ApprovalStatus ApprovalStatus, bool IsActive, bool IsPremium, int TotalOrders, decimal TotalSpendUsd, DateTimeOffset CreatedAt);

public sealed class GetBuyersQueryHandler : IQueryHandler<GetBuyersQuery, ErrorOr<PagedResult<BuyerListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBuyersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PagedResult<BuyerListItemDto>>> Handle(GetBuyersQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list buyers.");

        var (page, pageSize) = PagedResult<BuyerListItemDto>.Normalize(query.Page, query.PageSize);

        var baseQuery =
            from buyer in _db.Buyers.AsNoTracking()
            join profile in _db.Profiles.AsNoTracking() on buyer.Id equals profile.Id
            select new { buyer, profile };

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .OrderByDescending(x => x.buyer.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new BuyerListItemDto(
                x.buyer.Id, x.profile.Email, x.buyer.CompanyName, x.buyer.Country, x.buyer.City,
                x.profile.ApprovalStatus, x.profile.IsActive, x.buyer.IsPremium, x.buyer.TotalOrders, x.buyer.TotalSpendUsd, x.buyer.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<BuyerListItemDto>(items, totalCount, page, pageSize);
    }
}
