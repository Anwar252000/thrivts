using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Influencers;

public sealed record GetInfluencersQuery : IQuery<ErrorOr<List<InfluencerListItemDto>>>;

public sealed record InfluencerListItemDto(
    Guid Id, string InfluencerCode, string ReferralCode, string? FullName, string? Email,
    decimal CommissionRate, string Status, DateTimeOffset CreatedAt);

public sealed class GetInfluencersQueryHandler : IQueryHandler<GetInfluencersQuery, ErrorOr<List<InfluencerListItemDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetInfluencersQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<InfluencerListItemDto>>> Handle(GetInfluencersQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can list influencers.");

        var result = await _db.Influencers.AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InfluencerListItemDto(i.Id, i.InfluencerCode, i.ReferralCode, i.FullName, i.Email, i.CommissionRate, i.Status, i.CreatedAt))
            .ToListAsync(cancellationToken);

        return result;
    }
}
