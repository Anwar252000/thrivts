using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Commissions;

/// <summary>Replaces get_platform_fee_revenue / the platform_fee_revenue view — monthly platform
/// fee revenue from paid-out deal allocations.</summary>
public sealed record GetPlatformFeeRevenueQuery : IQuery<ErrorOr<List<PlatformFeeRevenueMonthDto>>>;

public sealed record PlatformFeeRevenueMonthDto(
    int Year, int Month, int Allocations, long Pcs, decimal FeeRevenueUsd, decimal GrossPaidUsd, decimal NetPaidToSellersUsd);

public sealed class GetPlatformFeeRevenueQueryHandler : IQueryHandler<GetPlatformFeeRevenueQuery, ErrorOr<List<PlatformFeeRevenueMonthDto>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetPlatformFeeRevenueQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<List<PlatformFeeRevenueMonthDto>>> Handle(GetPlatformFeeRevenueQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view platform fee revenue.");

        var paidAllocations = await _db.DealAllocations.AsNoTracking()
            .Where(a => a.PayoutPaidAt != null)
            .Select(a => new
            {
                a.PayoutPaidAt,
                a.AllocatedQuantityPcs,
                PlatformFeeUsd = a.PlatformFeeUsd ?? 0,
                GrossPayoutUsd = a.GrossPayoutUsd ?? 0,
                a.TotalPayoutUsd
            })
            .ToListAsync(cancellationToken);

        var result = paidAllocations
            .GroupBy(a => new { a.PayoutPaidAt!.Value.Year, a.PayoutPaidAt.Value.Month })
            .OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month)
            .Select(g => new PlatformFeeRevenueMonthDto(
                g.Key.Year, g.Key.Month, g.Count(), g.Sum(a => (long)a.AllocatedQuantityPcs),
                g.Sum(a => a.PlatformFeeUsd), g.Sum(a => a.GrossPayoutUsd), g.Sum(a => a.TotalPayoutUsd)))
            .ToList();

        return result;
    }
}
