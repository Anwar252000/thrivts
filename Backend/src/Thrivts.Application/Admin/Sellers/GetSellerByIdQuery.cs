using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Sellers;

/// <summary>Admin-only — includes the seller's real identity, unlike any buyer-facing projection.</summary>
public sealed record GetSellerByIdQuery(Guid SellerId) : IQuery<ErrorOr<SellerDetailDto>>;

public sealed record SellerDetailDto(
    Guid Id, string Email, string? Phone, string? WhatsApp, string PublicAlias, string? CompanyName,
    string LocationCity, string LocationCountry, int? YearsInBusiness, string[] CategoriesSupplied,
    string? ReferenceContact, SellerTier Tier, string[]? Tags, string? SellerCode,
    bool KycVerified, DateTimeOffset? KycVerifiedAt, string? KycNotes,
    ApprovalStatus ApprovalStatus, bool IsActive, int TotalOrdersFulfilled, long TotalPcsSupplied,
    decimal TotalPaidUsd, int DisputeCount, int BackoutCount, string? Notes, DateTimeOffset CreatedAt);

public sealed class GetSellerByIdQueryHandler : IQueryHandler<GetSellerByIdQuery, ErrorOr<SellerDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetSellerByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<SellerDetailDto>> Handle(GetSellerByIdQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view seller details.");

        var result = await (
                from seller in _db.Sellers.AsNoTracking()
                join profile in _db.Profiles.AsNoTracking() on seller.Id equals profile.Id
                where seller.Id == query.SellerId
                select new SellerDetailDto(
                    seller.Id, profile.Email, seller.Phone, seller.WhatsApp, seller.PublicAlias, seller.CompanyName,
                    seller.LocationCity, seller.LocationCountry, seller.YearsInBusiness, seller.CategoriesSupplied,
                    seller.ReferenceContact, seller.Tier, seller.Tags, seller.SellerCode,
                    seller.KycVerified, seller.KycVerifiedAt, seller.KycNotes,
                    profile.ApprovalStatus, profile.IsActive, seller.TotalOrdersFulfilled, seller.TotalPcsSupplied,
                    seller.TotalPaidUsd, seller.DisputeCount, seller.BackoutCount, seller.Notes, seller.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? Error.NotFound(description: $"Seller '{query.SellerId}' was not found.")
            : result;
    }
}
