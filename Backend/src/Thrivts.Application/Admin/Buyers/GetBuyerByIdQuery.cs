using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Buyers;

public sealed record GetBuyerByIdQuery(Guid BuyerId) : IQuery<ErrorOr<BuyerDetailDto>>;

public sealed record BuyerDetailDto(
    Guid Id, string Email, string? Phone, string? WhatsApp, string CompanyName, string? CompanyRegistration,
    string? VatId, string Country, string? City, string? Website, string? Instagram,
    int? EstimatedMonthlyVolumePcs, string[]? CategoriesOfInterest, Guid? AttributedToAgency,
    Guid? InfluencerId, DateTimeOffset? FirstOrderAt, ApprovalStatus ApprovalStatus, bool IsActive,
    bool IsPremium, int TotalOrders, decimal TotalSpendUsd, string? Notes, DateTimeOffset CreatedAt);

public sealed class GetBuyerByIdQueryHandler : IQueryHandler<GetBuyerByIdQuery, ErrorOr<BuyerDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetBuyerByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<BuyerDetailDto>> Handle(GetBuyerByIdQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view buyer details.");

        var result = await (
                from buyer in _db.Buyers.AsNoTracking()
                join profile in _db.Profiles.AsNoTracking() on buyer.Id equals profile.Id
                where buyer.Id == query.BuyerId
                select new BuyerDetailDto(
                    buyer.Id, profile.Email, profile.Phone, profile.WhatsApp, buyer.CompanyName, buyer.CompanyRegistration,
                    buyer.VatId, buyer.Country, buyer.City, buyer.Website, buyer.Instagram,
                    buyer.EstimatedMonthlyVolumePcs, buyer.CategoriesOfInterest, buyer.AttributedToAgency,
                    buyer.InfluencerId, buyer.FirstOrderAt, profile.ApprovalStatus, profile.IsActive,
                    buyer.IsPremium, buyer.TotalOrders, buyer.TotalSpendUsd, buyer.Notes, buyer.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return result is null
            ? Error.NotFound(description: $"Buyer '{query.BuyerId}' was not found.")
            : result;
    }
}
