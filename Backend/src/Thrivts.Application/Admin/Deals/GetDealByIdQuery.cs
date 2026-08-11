using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

public sealed record GetDealByIdQuery(Guid DealId) : IQuery<ErrorOr<DealDetailDto>>;

public sealed record DealDetailDto(
    Guid Id, string DealNumber, Guid RequirementId, Guid BuyerId, Guid? SellerId, Guid? AgencyId,
    DealStatus Status, int TotalQuantityPcs,
    decimal BuyerPricePerPcUsd, decimal AvgSellerPricePerPcUsd, decimal SpreadPerPcUsd,
    decimal SubtotalUsd, decimal? ShippingCostUsd, decimal TotalInvoiceUsd, decimal TotalSpreadUsd, decimal TotalSellerPayoutUsd,
    string? DestinationCountry, string? DestinationPort,
    string? ContainerNumber, string? ContainerSize, string? ShippingLine, string? VesselName, string? BillOfLading,
    string? TrackingNumber, string? TrackingUrl, string? Courier,
    string? PaymentMethod, string? PaymentReference, DateTimeOffset? PaymentReceivedAt,
    bool HasDispute, decimal DisputeAmountUsd, string? AdminNotes,
    DateTimeOffset? ConfirmedAt, DateTimeOffset? PaidAt, DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt, DateTimeOffset? SettledAt, DateTimeOffset? CancelledAt, string? CancellationReason,
    DateTimeOffset CreatedAt);

public sealed class GetDealByIdQueryHandler : IQueryHandler<GetDealByIdQuery, ErrorOr<DealDetailDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetDealByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<DealDetailDto>> Handle(GetDealByIdQuery query, CancellationToken cancellationToken)
    {
        if (_currentUser.Role != UserRole.Admin)
            return Error.Forbidden(description: "Only admins can view deal details.");

        var deal = await _db.Deals.AsNoTracking().FirstOrDefaultAsync(d => d.Id == query.DealId, cancellationToken);
        if (deal is null)
            return Error.NotFound(description: $"Deal '{query.DealId}' was not found.");

        return new DealDetailDto(
            deal.Id, deal.DealNumber, deal.RequirementId, deal.BuyerId, deal.SellerId, deal.AgencyId,
            deal.Status, deal.TotalQuantityPcs,
            deal.BuyerPricePerPcUsd, deal.AvgSellerPricePerPcUsd, deal.SpreadPerPcUsd,
            deal.SubtotalUsd, deal.ShippingCostUsd, deal.TotalInvoiceUsd, deal.TotalSpreadUsd, deal.TotalSellerPayoutUsd,
            deal.DestinationCountry, deal.DestinationPort,
            deal.ContainerNumber, deal.ContainerSize, deal.ShippingLine, deal.VesselName, deal.BillOfLading,
            deal.TrackingNumber, deal.TrackingUrl, deal.Courier,
            deal.PaymentMethod, deal.PaymentReference, deal.PaymentReceivedAt,
            deal.HasDispute, deal.DisputeAmountUsd, deal.AdminNotes,
            deal.ConfirmedAt, deal.PaidAt, deal.DispatchedAt,
            deal.DeliveredAt, deal.SettledAt, deal.CancelledAt, deal.CancellationReason,
            deal.CreatedAt);
    }
}
