using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Powers the buyer portal's read-only Profile section.</summary>
public sealed record GetMyBuyerProfileQuery : IQuery<ErrorOr<BuyerProfileDto>>;

public sealed record BuyerProfileDto(
    string FullName, string Email, string? Phone, string? WhatsApp, LanguagePref Language,
    string CompanyName, string? Website, string Country, string? City, string? Instagram,
    int? EstimatedMonthlyVolumePcs, string? TypicalRequirementType, ApprovalStatus ApprovalStatus, DateTimeOffset CreatedAt);

public sealed class GetMyBuyerProfileQueryHandler : IQueryHandler<GetMyBuyerProfileQuery, ErrorOr<BuyerProfileDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyBuyerProfileQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<BuyerProfileDto>> Handle(GetMyBuyerProfileQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == _currentUser.UserId, cancellationToken);
        var buyer = await _db.Buyers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == _currentUser.UserId, cancellationToken);
        if (profile is null || buyer is null)
            return Error.NotFound(description: "Buyer profile was not found.");

        return new BuyerProfileDto(
            profile.FullName, profile.Email, profile.Phone, profile.WhatsApp, profile.LanguagePref,
            buyer.CompanyName, buyer.Website, buyer.Country, buyer.City, buyer.Instagram,
            buyer.EstimatedMonthlyVolumePcs, buyer.TypicalRequirementType, profile.ApprovalStatus, profile.CreatedAt);
    }
}
