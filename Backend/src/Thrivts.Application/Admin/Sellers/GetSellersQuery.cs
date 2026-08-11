using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

/// <summary>Admin-only — includes the seller's real identity (CompanyName), unlike any
/// buyer-facing projection. Never reuse this DTO shape on a buyer-facing endpoint.</summary>
public sealed record GetSellersQuery(int Page, int PageSize) : IQuery<ErrorOr<PagedResult<SellerListItemDto>>>;

public sealed record SellerListItemDto(
    Guid Id, string Email, string PublicAlias, string? CompanyName, string LocationCity, string LocationCountry,
    SellerTier Tier, bool KycVerified, ApprovalStatus ApprovalStatus, bool IsActive,
    int TotalOrdersFulfilled, decimal TotalPaidUsd, DateTimeOffset CreatedAt);

public sealed class GetSellersQueryHandler : IQueryHandler<GetSellersQuery, ErrorOr<PagedResult<SellerListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSellersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<PagedResult<SellerListItemDto>>> Handle(GetSellersQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list sellers.");

        var (page, pageSize) = PagedResult<SellerListItemDto>.Normalize(query.Page, query.PageSize);

        var baseQuery =
            from seller in _db.Sellers.AsNoTracking()
            join profile in _db.Profiles.AsNoTracking() on seller.Id equals profile.Id
            select new { seller, profile };

        var totalCount = await baseQuery.CountAsync(cancellationToken);
        var items = await baseQuery
            .OrderByDescending(x => x.seller.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SellerListItemDto(
                x.seller.Id, x.profile.Email, x.seller.PublicAlias, x.seller.CompanyName, x.seller.LocationCity, x.seller.LocationCountry,
                x.seller.Tier, x.seller.KycVerified, x.profile.ApprovalStatus, x.profile.IsActive,
                x.seller.TotalOrdersFulfilled, x.seller.TotalPaidUsd, x.seller.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<SellerListItemDto>(items, totalCount, page, pageSize);
    }
}
