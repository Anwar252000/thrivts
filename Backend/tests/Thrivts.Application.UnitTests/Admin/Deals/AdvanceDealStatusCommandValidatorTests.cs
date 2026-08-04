using AwesomeAssertions;
using Thrivts.Application.Admin.Deals;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.UnitTests.Admin.Deals;

public class AdvanceDealStatusCommandValidatorTests
{
    private readonly AdvanceDealStatusCommandValidator _validator = new();

    [Fact]
    public void Fails_when_DealId_is_empty()
    {
        var result = _validator.Validate(new AdvanceDealStatusCommand(Guid.Empty, DealStatus.Confirmed));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AdvanceDealStatusCommand.DealId));
    }

    [Fact]
    public void Passes_for_a_valid_command()
    {
        var result = _validator.Validate(new AdvanceDealStatusCommand(Guid.NewGuid(), DealStatus.Confirmed));

        result.IsValid.Should().BeTrue();
    }
}
