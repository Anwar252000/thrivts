using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Requirements;

/// <summary>
/// The list/bid-board queries never expose BuyerTargetPriceUsd or AdminNotes, so the admin edit
/// form had nothing to pre-fill them from (and would have silently zeroed the price on save).
/// </summary>
public sealed record GetRequirementByIdQuery(Guid RequirementId) : IQuery<ErrorOr<RequirementDetailDto>>;

public sealed record RequirementDetailDto(
    Guid Id, string RequirementNumber, Guid BuyerId, string ItemName, int QuantityPcs, GradeType Grade,
    string DestinationCountry, string? DestinationPort, decimal BuyerTargetPriceUsd, decimal? SellerTargetPriceUsd,
    string? BuyerNotes, string? AdminNotes, RequirementStatus Status, bool PublicDisplay, DateTimeOffset CreatedAt,
    SellerTier MinSellerTier, string[]? RestrictedToTags);

public sealed class GetRequirementByIdQueryHandler : IQueryHandler<GetRequirementByIdQuery, ErrorOr<RequirementDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetRequirementByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<RequirementDetailDto>> Handle(GetRequirementByIdQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view requirement details.");

        var requirement = await _db.Requirements.AsNoTracking().FirstOrDefaultAsync(r => r.Id == query.RequirementId, cancellationToken);
        if (requirement is null)
            return Error.NotFound(description: $"Requirement '{query.RequirementId}' was not found.");

        return new RequirementDetailDto(
            requirement.Id, requirement.RequirementNumber, requirement.BuyerId, requirement.ItemName, requirement.QuantityPcs, requirement.Grade,
            requirement.DestinationCountry, requirement.DestinationPort, requirement.BuyerTargetPriceUsd, requirement.SellerTargetPriceUsd,
            requirement.BuyerNotes, requirement.AdminNotes, requirement.Status, requirement.PublicDisplay, requirement.CreatedAt,
            requirement.MinSellerTier, requirement.RestrictedToTags);
    }
}
