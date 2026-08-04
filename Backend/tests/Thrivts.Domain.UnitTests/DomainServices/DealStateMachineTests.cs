using AwesomeAssertions;
using Thrivts.Domain.DomainServices;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.UnitTests.DomainServices;

public class DealStateMachineTests
{
    [Theory]
    [InlineData(DealStatus.Match, DealStatus.Confirmed, true)]
    [InlineData(DealStatus.Confirmed, DealStatus.Paid, true)]
    [InlineData(DealStatus.Delivered, DealStatus.Settled, true)]
    [InlineData(DealStatus.Settled, DealStatus.Confirmed, false)]
    [InlineData(DealStatus.Match, DealStatus.Delivered, false)]
    [InlineData(DealStatus.Cancelled, DealStatus.Confirmed, false)]
    public void CanTransition_matches_the_documented_lifecycle(DealStatus from, DealStatus to, bool expected)
    {
        DealStateMachine.CanTransition(from, to).Should().Be(expected);
    }

    [Fact]
    public void EnsureValidTransition_throws_a_domain_exception_for_an_invalid_transition()
    {
        var act = () => DealStateMachine.EnsureValidTransition(DealStatus.Settled, DealStatus.Paid);

        act.Should().Throw<Thrivts.Domain.Exceptions.DomainException>();
    }
}
