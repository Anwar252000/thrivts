using AwesomeAssertions;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.UnitTests.Entities;

public class DealTests
{
    private static Deal CreateDeal() =>
        new("DEAL-0001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), totalQuantityPcs: 100,
            buyerPricePerPcUsd: 13m, avgSellerPricePerPcUsd: 12.30m, spreadPerPcUsd: 0.70m,
            subtotalUsd: 1300m, totalInvoiceUsd: 1300m, totalSpreadUsd: 70m, totalSellerPayoutUsd: 1230m);

    [Fact]
    public void AdvanceTo_stamps_the_matching_timestamp()
    {
        var deal = CreateDeal();
        var now = DateTimeOffset.UtcNow;

        deal.AdvanceTo(DealStatus.Confirmed, now);
        deal.AdvanceTo(DealStatus.AwaitingPayment, now);
        deal.AdvanceTo(DealStatus.Paid, now);

        deal.Status.Should().Be(DealStatus.Paid);
        deal.PaidAt.Should().Be(now);
    }

    [Fact]
    public void AdvanceTo_throws_on_an_invalid_transition_instead_of_silently_no_opping()
    {
        var deal = CreateDeal();

        var act = () => deal.AdvanceTo(DealStatus.Settled, DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>();
        deal.Status.Should().Be(DealStatus.Draft);
    }

    [Fact]
    public void Cancel_records_the_reason()
    {
        var deal = CreateDeal();

        deal.Cancel("Buyer withdrew", DateTimeOffset.UtcNow);

        deal.Status.Should().Be(DealStatus.Cancelled);
        deal.CancellationReason.Should().Be("Buyer withdrew");
    }
}
