using AwesomeAssertions;
using Thrivts.Domain.Exceptions;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;
using Thrivts.Domain.Wash;

namespace Thrivts.Domain.UnitTests.Lots;

public class CorrectionAndWashTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 7);

    private static (Lot Lot, LotItem Line) FinalizedLot(int a = 60, int c = 30, int rejected = 10)
    {
        var lot = new Lot("VEWL01", Guid.NewGuid(), "Khurram", 5, Today, Guid.NewGuid(), Now);
        var line = lot.AddItem(null, "TNF Fleece Regular", a + c + rejected);
        lot.AddItem(null, "Carhartt Jacket", 10);
        lot.FinalizeCount(Now);
        lot.SetGrades(line.Id, a, 0, c, rejected, rejected > 0 ? RejectionReason.PakkaDaagh : null);
        lot.SetGrades(lot.Items[1].Id, 10, 0, 0, 0, null);
        lot.SignBySupervisor(Guid.NewGuid(), Guid.NewGuid(), Now);
        lot.Cosign(Guid.NewGuid(), Guid.NewGuid(), Now);
        return (lot, line);
    }

    private static StockBalance Balance(Lot lot, LotItem line, Grade grade, int qty) =>
        new(lot.Id, line.Id, null, lot.Serial, lot.SupplierId, lot.SupplierName, lot.ArrivalDate, line.ItemName, grade, qty, Now);

    [Fact]
    public void Correction_returns_stock_deltas_and_keeps_the_line_balanced()
    {
        var (lot, line) = FinalizedLot();

        var (deltaA, deltaC) = lot.CorrectFinalizedItem(line.Id, 55, 32, 13, null);

        deltaA.Should().Be(-5);
        deltaC.Should().Be(2);
        line.Received.Should().Be(100);
        line.Unaccounted.Should().Be(0);
        line.RejectionReason.Should().Be(RejectionReason.PakkaDaagh, "the original reason carries over");
    }

    [Fact]
    public void Correction_is_only_for_finalized_lots()
    {
        var lot = new Lot("VEWL02", Guid.NewGuid(), "Khurram", 5, Today, Guid.NewGuid(), Now);
        var line = lot.AddItem(null, "TNF Fleece Regular", 10);

        var act = () => lot.CorrectFinalizedItem(line.Id, 10, 0, 0, null);

        act.Should().Throw<DomainException>().WithMessage("*finalized*");
    }

    [Fact]
    public void Correction_cannot_drop_rejections_already_returned_to_the_supplier()
    {
        var (lot, line) = FinalizedLot(rejected: 10);
        line.RecordReturn(8);

        var act = () => lot.CorrectFinalizedItem(line.Id, 65, 30, 5, null);

        act.Should().Throw<DomainException>().WithMessage("*already returned*");
    }

    [Fact]
    public void A_finalized_lot_keeps_at_least_one_item()
    {
        var (lot, line) = FinalizedLot();
        lot.RemoveFinalizedItem(lot.Items[1].Id);

        var act = () => lot.RemoveFinalizedItem(line.Id);

        act.Should().Throw<DomainException>().WithMessage("*only item*");
    }

    [Fact]
    public void Wash_rejections_add_to_what_is_owed_back_to_the_supplier()
    {
        var (_, line) = FinalizedLot(rejected: 10);

        line.ApplyWashRejected(4);

        line.TotalRejected.Should().Be(14);
        line.PendingRejections.Should().Be(14);
        line.Rejected.Should().Be(10, "the grading record is not rewritten");
    }

    [Fact]
    public void Wash_rejections_cannot_be_corrected_below_what_was_returned()
    {
        var (_, line) = FinalizedLot(rejected: 0);
        line.ApplyWashRejected(5);
        line.RecordReturn(5);

        var act = () => line.ApplyWashRejected(-2);

        act.Should().Throw<DomainException>().WithMessage("*already returned*");
    }

    [Fact]
    public void Wash_keeps_one_lot_and_only_receives_what_was_sent()
    {
        var (lot, line) = FinalizedLot();
        var vendor = new WashVendor("Irfan");
        var batch = new WashBatch("VEWL01-W1", lot.Id, lot.Serial, vendor, Today, null);
        var washLine = batch.AddLine(Balance(lot, line, Grade.C, 30), "Fleeces", 20, 15);

        var otherLot = new Lot("VEWL09", Guid.NewGuid(), "Nasir", 1, Today, Guid.NewGuid(), Now);
        var otherLine = otherLot.AddItem(null, "Hoodie", 5);
        var mixLots = () => batch.AddLine(Balance(otherLot, otherLine, Grade.A, 5), "Hoodies", 5, 10);
        var tooMany = () => batch.Receive(washLine.Id, 15, 5, 1, 15, "Fleeces", Today);

        mixLots.Should().Throw<DomainException>().WithMessage("*One lot per wash*");
        tooMany.Should().Throw<DomainException>().WithMessage("*more than the 20 sent*");
    }

    [Fact]
    public void Receiving_is_cumulative_and_returns_the_change_since_last_save()
    {
        var (lot, line) = FinalizedLot();
        var batch = new WashBatch("VEWL01-W1", lot.Id, lot.Serial, new WashVendor("Irfan"), Today, null);
        var washLine = batch.AddLine(Balance(lot, line, Grade.C, 30), "Fleeces", 20, 15);

        batch.Receive(washLine.Id, 10, 2, 1, 15, "Fleeces", Today);
        batch.Status.Should().Be(WashStatus.Partial);
        batch.StillOut.Should().Be(7);

        var delta = batch.Receive(washLine.Id, 12, 6, 2, 15, "Fleeces", Today);

        delta.DeltaA.Should().Be(2);
        delta.DeltaC.Should().Be(4);
        delta.DeltaRejected.Should().Be(1);
        batch.Status.Should().Be(WashStatus.Received);
        batch.AmountDue.Should().Be(18 * 15m, "the washer is paid only for good pieces");
    }

    [Fact]
    public void Finalizing_for_payment_needs_every_piece_back_and_locks_the_wash()
    {
        var (lot, line) = FinalizedLot();
        var batch = new WashBatch("VEWL01-W1", lot.Id, lot.Serial, new WashVendor("Irfan"), Today, null);
        var washLine = batch.AddLine(Balance(lot, line, Grade.A, 60), "Fleeces", 10, 15);
        batch.Receive(washLine.Id, 8, 0, 0, 15, "Fleeces", Today);

        var early = () => batch.FinalizeForPayment(Guid.NewGuid(), Now);
        early.Should().Throw<DomainException>().WithMessage("*still at the washer*");

        batch.Receive(washLine.Id, 9, 0, 1, 15, "Fleeces", Today);
        batch.FinalizeForPayment(Guid.NewGuid(), Now);
        var edit = () => batch.Receive(washLine.Id, 10, 0, 0, 15, "Fleeces", Today);

        edit.Should().Throw<DomainException>().WithMessage("*reopen*");
        batch.Reopen();
        batch.Status.Should().Be(WashStatus.Received);
    }

    [Fact]
    public void A_wash_with_pieces_received_cannot_be_cancelled()
    {
        var (lot, line) = FinalizedLot();
        var batch = new WashBatch("VEWL01-W1", lot.Id, lot.Serial, new WashVendor("Irfan"), Today, null);
        var washLine = batch.AddLine(Balance(lot, line, Grade.A, 60), "Fleeces", 10, 15);
        batch.Receive(washLine.Id, 1, 0, 0, 15, "Fleeces", Today);

        var act = batch.EnsureCancellable;

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("TNF Puffer Jacket", "Puffers")]
    [InlineData("Carhartt Jacket", "Jackets")]
    [InlineData("Nike Hoodie", "Hoodies")]
    [InlineData("Levi's Denim", "Jeans")]
    [InlineData("Ralph Lauren Polo Shirt", "Polos")]
    [InlineData("Mystery bundle", "Mix")]
    public void Wash_categories_follow_the_legacy_keyword_rules(string item, string expected) =>
        WashCategories.Classify(item).Should().Be(expected);
}
