using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Sellers;

/// <summary>Powers the seller portal's read-only Profile section — mirrors seller.html's renderProfile().</summary>
public sealed record GetMySellerProfileQuery : IQuery<ErrorOr<SellerProfileDto>>;

public sealed record SellerProfileDto(
    string? SellerCode, bool KycVerified, string? CompanyName, string LocationCity, string LocationCountry,
    string? Phone, string? WhatsApp, SellerTier Tier, string[]? Tags, string[] CategoriesSupplied, DateTimeOffset CreatedAt);

public sealed class GetMySellerProfileQueryHandler : IQueryHandler<GetMySellerProfileQuery, ErrorOr<SellerProfileDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMySellerProfileQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<SellerProfileDto>> Handle(GetMySellerProfileQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var seller = await _db.Sellers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == _currentUser.UserId, cancellationToken);
        if (seller is null)
            return Error.NotFound(description: "Seller profile was not found.");

        return new SellerProfileDto(
            seller.SellerCode, seller.KycVerified, seller.CompanyName, seller.LocationCity, seller.LocationCountry,
            seller.Phone, seller.WhatsApp, seller.Tier, seller.Tags, seller.CategoriesSupplied, seller.CreatedAt);
    }
}
