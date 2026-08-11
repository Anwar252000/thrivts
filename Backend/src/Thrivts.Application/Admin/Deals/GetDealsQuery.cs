using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

public sealed record GetDealsQuery(DealStatus? Status, int Page, int PageSize) : IQuery<ErrorOr<PagedResult<DealListItemDto>>>;

public sealed record DealListItemDto(
    Guid Id, string DealNumber, Guid BuyerId, Guid? SellerId, int TotalQuantityPcs,
    decimal TotalInvoiceUsd, decimal TotalSpreadUsd, DealStatus Status, bool HasDispute, DateTimeOffset CreatedAt);

public sealed class GetDealsQueryHandler : IQueryHandler<GetDealsQuery, ErrorOr<PagedResult<DealListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDealsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PagedResult<DealListItemDto>>> Handle(GetDealsQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list deals.");

        var (page, pageSize) = PagedResult<DealListItemDto>.Normalize(query.Page, query.PageSize);

        var deals = _db.Deals.AsNoTracking();
        if (query.Status is not null)
            deals = deals.Where(d => d.Status == query.Status);

        var totalCount = await deals.CountAsync(cancellationToken);
        var items = await deals
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DealListItemDto(
                d.Id, d.DealNumber, d.BuyerId, d.SellerId, d.TotalQuantityPcs,
                d.TotalInvoiceUsd, d.TotalSpreadUsd, d.Status, d.HasDispute, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<DealListItemDto>(items, totalCount, page, pageSize);
    }
}
