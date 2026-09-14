using Thrivts.Domain.Common;
using Thrivts.Domain.DomainServices;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Entities;

public class Deal : BaseEntity, IAggregateRoot
{
    public string DealNumber { get; private set; } = default!;
    public Guid RequirementId { get; private set; }
    public Guid BuyerId { get; private set; }
    public Guid? SellerId { get; private set; }
    public Guid? AgencyId { get; private set; }
    public Guid? SourceResponseId { get; private set; }
    public Guid? SourceOfferId { get; private set; }
    public Guid? AcceptedOfferId { get; private set; }
    public Guid? CreatedBy { get; private set; }

    public DealStatus Status { get; private set; } = DealStatus.Draft;
    public int TotalQuantityPcs { get; private set; }

    public decimal BuyerPricePerPcUsd { get; private set; }
    public decimal AvgSellerPricePerPcUsd { get; private set; }
    public decimal? SellerCostPerPcUsd { get; private set; }
    public decimal SpreadPerPcUsd { get; private set; }
    public decimal? SpreadUsd { get; private set; }
    public decimal SubtotalUsd { get; private set; }
    public decimal? ShippingCostUsd { get; private set; }
    public string? ShippingQuoteCurrency { get; private set; }
    public decimal TotalInvoiceUsd { get; private set; }
    public decimal TotalSpreadUsd { get; private set; }
    public decimal? NetSettledSpreadUsd { get; private set; }
    public decimal TotalSellerPayoutUsd { get; private set; }
    public decimal? TotalSellerCostUsd { get; private set; }
    public string? ExchangeRateSnapshotJson { get; private set; }

    public string? DestinationCountry { get; private set; }
    public string? DestinationPort { get; private set; }

    public DateTimeOffset? PaymentReceivedAt { get; private set; }
    public string? PaymentMethod { get; private set; }
    public string? PaymentReference { get; private set; }
    public Guid? PaymentReceivedBy { get; private set; }
    public DateTimeOffset? SellersPaidAt { get; private set; }
    public DateTimeOffset? SellerMarkedReadyAt { get; private set; }

    public string? ContainerNumber { get; private set; }
    public string? ContainerSize { get; private set; }
    public string? ShippingLine { get; private set; }
    public string? VesselName { get; private set; }
    public string? BillOfLading { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string? TrackingUrl { get; private set; }
    public string? Courier { get; private set; }
    public DateOnly? EstimatedDispatchDate { get; private set; }
    public DateOnly? EstimatedArrivalDate { get; private set; }
    public DateTimeOffset? ExpectedDeliveryAt { get; private set; }

    public bool HasDispute { get; private set; }
    public decimal DisputeAmountUsd { get; private set; }
    public string? AdminNotes { get; private set; }

    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? InFulfillmentAt { get; private set; }
    public DateTimeOffset? DispatchedAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset? CommissionReleaseDueAt { get; private set; }
    public string? CancellationReason { get; private set; }

    private Deal()
    {
        // EF Core
    }

    public Deal(string dealNumber, Guid requirementId, Guid buyerId, Guid? sellerId, int totalQuantityPcs,
        decimal buyerPricePerPcUsd, decimal avgSellerPricePerPcUsd, decimal spreadPerPcUsd,
        decimal subtotalUsd, decimal totalInvoiceUsd, decimal totalSpreadUsd, decimal totalSellerPayoutUsd,
        Guid? agencyId = null, Guid? sourceResponseId = null, Guid? sourceOfferId = null, Guid? createdBy = null)
    {
        DealNumber = dealNumber;
        RequirementId = requirementId;
        BuyerId = buyerId;
        SellerId = sellerId;
        TotalQuantityPcs = totalQuantityPcs;
        BuyerPricePerPcUsd = buyerPricePerPcUsd;
        AvgSellerPricePerPcUsd = avgSellerPricePerPcUsd;
        SpreadPerPcUsd = spreadPerPcUsd;
        SubtotalUsd = subtotalUsd;
        TotalInvoiceUsd = totalInvoiceUsd;
        TotalSpreadUsd = totalSpreadUsd;
        TotalSellerPayoutUsd = totalSellerPayoutUsd;
        AgencyId = agencyId;
        SourceResponseId = sourceResponseId;
        SourceOfferId = sourceOfferId;
        CreatedBy = createdBy;
    }

    /// <summary>Replaces the old advance_deal_status RPC. Invalid transitions throw rather than silently no-op.</summary>
    public void AdvanceTo(DealStatus next, DateTimeOffset occurredAt)
    {
        DealStateMachine.EnsureValidTransition(Status, next);

        Status = next;
        switch (next)
        {
            case DealStatus.Confirmed:
                ConfirmedAt = occurredAt;
                break;
            case DealStatus.Paid:
                PaidAt = occurredAt;
                PaymentReceivedAt = occurredAt;
                break;
            case DealStatus.InFulfillment:
                InFulfillmentAt = occurredAt;
                break;
            case DealStatus.Dispatched:
                DispatchedAt = occurredAt;
                break;
            case DealStatus.Delivered:
                DeliveredAt = occurredAt;
                break;
            case DealStatus.Settled:
                SettledAt = occurredAt;
                break;
        }
    }

    public void Cancel(string reason, DateTimeOffset occurredAt)
    {
        DealStateMachine.EnsureValidTransition(Status, DealStatus.Cancelled);
        Status = DealStatus.Cancelled;
        CancellationReason = reason;
        CancelledAt = occurredAt;
    }

    public void RecordPayment(string method, string? reference, Guid receivedBy, DateTimeOffset occurredAt)
    {
        PaymentMethod = method;
        PaymentReference = reference;
        PaymentReceivedBy = receivedBy;
        PaymentReceivedAt = occurredAt;
    }

    public void RecordSellersPaid(DateTimeOffset occurredAt) => SellersPaidAt = occurredAt;

    /// <summary>Replaces seller_mark_order_ready — seller signals the order is packed and ready for
    /// pickup. Idempotent (a second call is a harmless no-op on the timestamp); advances Paid ->
    /// InFulfillment the same way the RPC does, via the existing state machine.</summary>
    public void MarkOrderReady(DateTimeOffset occurredAt)
    {
        if (Status is not (DealStatus.Paid or DealStatus.InFulfillment))
            throw new DomainException($"Order can only be marked ready once payment is received (status is {Status}).");

        SellerMarkedReadyAt ??= occurredAt;

        if (Status == DealStatus.Paid)
            AdvanceTo(DealStatus.InFulfillment, occurredAt);
    }

    public void SetShippingDetails(string? containerNumber, string? containerSize, string? shippingLine,
        string? vesselName, string? billOfLading)
    {
        ContainerNumber = containerNumber;
        ContainerSize = containerSize;
        ShippingLine = shippingLine;
        VesselName = vesselName;
        BillOfLading = billOfLading;
    }

    public void SetTrackingDetails(string? trackingNumber, string? trackingUrl, string? courier)
    {
        TrackingNumber = trackingNumber;
        TrackingUrl = trackingUrl;
        Courier = courier;
    }

    public void SetEstimatedDates(DateOnly? dispatchDate, DateOnly? arrivalDate, DateTimeOffset? expectedDeliveryAt)
    {
        EstimatedDispatchDate = dispatchDate;
        EstimatedArrivalDate = arrivalDate;
        ExpectedDeliveryAt = expectedDeliveryAt;
    }

    public void FlagDispute(decimal disputeAmountUsd)
    {
        HasDispute = true;
        DisputeAmountUsd = disputeAmountUsd;
    }

    public void ClearDispute()
    {
        HasDispute = false;
        DisputeAmountUsd = 0;
    }

    public void SetCommissionReleaseDueAt(DateTimeOffset dueAt) => CommissionReleaseDueAt = dueAt;

    public void RecordSettlement(decimal netSettledSpreadUsd) => NetSettledSpreadUsd = netSettledSpreadUsd;
}
