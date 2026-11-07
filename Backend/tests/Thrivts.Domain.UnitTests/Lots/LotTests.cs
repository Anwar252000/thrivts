using AwesomeAssertions;
using Thrivts.Domain.Exceptions;
using Thrivts.Domain.Lots;

namespace Thrivts.Domain.UnitTests.Lots;

public class LotTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    private static Lot NewLot() => new("VEWL01", Guid.NewGuid(), "Khurram", 12, new DateOnly(2026, 11, 3), Guid.NewGuid(), Now);

    private static (Lot Lot, LotItem Line) GradingLot(int received = 100)
    {
        var lot = NewLot();
        var line = lot.AddItem(null, "TNF Fleece Regular", received);
        lot.FinalizeCount(Now);
        return (lot, line);
    }

    [Fact]
    public void Count_cannot_be_finalized_while_any_item_has_zero_received()
    {
        var lot = NewLot();
        lot.AddItem(null, "TNF Fleece Regular", 100);
        lot.AddItem(null, "RL Fleece", 0);

        var act = () => lot.FinalizeCount(Now);

        act.Should().Throw<DomainException>().WithMessage("*received count above 0*");
    }

    [Fact]
    public void Duplicate_items_are_rejected_case_insensitively()
    {
        var lot = NewLot();
        lot.AddItem(null, "TNF Fleece Regular", 10);

        var act = () => lot.AddItem(null, "  tnf   fleece regular ", 5);

        act.Should().Throw<DomainException>().WithMessage("*already in the lot*");
    }

    [Fact]
    public void Signing_requires_grade_B_to_be_zero()
    {
        var (lot, line) = GradingLot();
        lot.SetGrades(line.Id, 80, 10, 10, 0, null);

        var act = () => lot.SignBySupervisor(Guid.NewGuid(), Guid.NewGuid(), Now);

        act.Should().Throw<DomainException>().WithMessage("*Grade B must be 0*");
    }

    [Fact]
    public void Signing_requires_every_received_piece_to_be_graded()
    {
        var (lot, line) = GradingLot(100);
        lot.SetGrades(line.Id, 80, 0, 10, 0, null);

        var act = () => lot.SignBySupervisor(Guid.NewGuid(), Guid.NewGuid(), Now);

        act.Should().Throw<DomainException>().WithMessage("*10 pieces are not graded*");
    }

    [Fact]
    public void Signing_rejects_more_graded_pieces_than_received()
    {
        var (lot, line) = GradingLot(100);
        lot.SetGrades(line.Id, 95, 0, 10, 0, null);

        var act = () => lot.SignBySupervisor(Guid.NewGuid(), Guid.NewGuid(), Now);

        act.Should().Throw<DomainException>().WithMessage("*5 more pieces than were received*");
    }

    [Fact]
    public void Signing_requires_a_reason_for_rejected_pieces()
    {
        var (lot, line) = GradingLot(100);
        lot.SetGrades(line.Id, 80, 0, 10, 10, null);

        var act = () => lot.SignBySupervisor(Guid.NewGuid(), Guid.NewGuid(), Now);

        act.Should().Throw<DomainException>().WithMessage("*rejection reason*");
    }

    [Fact]
    public void Full_pipeline_reaches_finalized()
    {
        var (lot, line) = GradingLot(100);
        lot.SetGrades(line.Id, 80, 0, 10, 10, RejectionReason.PakkaDaagh);

        lot.SignBySupervisor(Guid.NewGuid(), Guid.NewGuid(), Now);
        lot.Stage.Should().Be(LotStage.AwaitingCosign);

        lot.Cosign(Guid.NewGuid(), Guid.NewGuid(), Now.AddHours(2));
        lot.Stage.Should().Be(LotStage.Finalized);
        line.Net.Should().Be(90);
    }

    [Fact]
    public void Sending_back_clears_the_supervisor_signature_and_records_why()
    {
        var (lot, line) = GradingLot(100);
        lot.SetGrades(line.Id, 100, 0, 0, 0, null);
        lot.SignBySupervisor(Guid.NewGuid(), Guid.NewGuid(), Now);

        lot.SendBack(Guid.NewGuid(), "Recount the fleece", Now.AddHours(1));

        lot.Stage.Should().Be(LotStage.Grading);
        lot.SupervisorSignatureId.Should().BeNull();
        lot.SentBackReason.Should().Be("Recount the fleece");
    }

    [Fact]
    public void Fix_count_keeps_the_grades_already_entered()
    {
        var (lot, line) = GradingLot(100);
        lot.SetGrades(line.Id, 60, 0, 30, 0, null);

        lot.ReopenCount();
        lot.SetReceived(line.Id, 90);

        lot.Stage.Should().Be(LotStage.Counting);
        line.QtyA.Should().Be(60);
        line.Received.Should().Be(90);
    }

    [Theory]
    [InlineData(500, 1)]
    [InlineData(1000, 1)]
    [InlineData(1001, 2)]
    [InlineData(3400, 4)]
    public void Grading_window_is_one_day_per_thousand_pieces(int received, int expectedDays)
    {
        var (lot, _) = GradingLot(received);

        lot.GradingDays.Should().Be(expectedDays);
        lot.GradeDueAt.Should().Be(Now.AddDays(expectedDays));
    }

    [Fact]
    public void Returns_cannot_exceed_pending_rejections()
    {
        var (lot, line) = GradingLot(100);
        lot.SetGrades(line.Id, 90, 0, 0, 10, RejectionReason.WrongStock);
        line.RecordReturn(6);

        var act = () => line.RecordReturn(5);

        act.Should().Throw<DomainException>().WithMessage("*only 4 rejected pieces*");
    }
}
