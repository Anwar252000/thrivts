using AwesomeAssertions;
using Thrivts.Domain.Identity;
using Thrivts.Domain.Operations;
using Thrivts.Domain.Sku;

namespace Thrivts.Domain.UnitTests;

public class AttendanceTests
{
    private static readonly DateOnly Monday = new(2026, 11, 2);
    private static readonly DateOnly Sunday = new(2026, 11, 1);

    [Theory]
    [InlineData(9, 0, 18, 0, 0)]
    [InlineData(9, 0, 20, 30, 2.5)]
    [InlineData(7, 0, 17, 0, 0)]
    [InlineData(19, 0, 21, 0, 2)]
    [InlineData(18, 0, 1, 0, 0)]
    public void Weekday_overtime_counts_only_time_after_six_pm(int inH, int inM, int outH, int outM, double expected) =>
        AttendanceRecord.ComputeOvertime(Monday, new TimeOnly(inH, inM), new TimeOnly(outH, outM)).Should().Be((decimal)expected);

    [Fact]
    public void Sunday_overtime_is_the_whole_shift() =>
        AttendanceRecord.ComputeOvertime(Sunday, new TimeOnly(9, 0), new TimeOnly(18, 0)).Should().Be(9m);
}

public class AppUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Five_wrong_pins_lock_the_account_for_thirty_seconds()
    {
        var user = new AppUser("Ali", "Gate Guard", UserRole.GateGuard, "#0EA5E9", "hash", 0);

        for (var i = 0; i < 4; i++)
            user.RecordFailedPin(Now);
        user.IsLockedOut(Now).Should().BeFalse();

        user.RecordFailedPin(Now);
        user.IsLockedOut(Now).Should().BeTrue();
        user.IsLockedOut(Now.AddSeconds(31)).Should().BeFalse();
    }

    [Fact]
    public void Lockout_grows_with_repeated_failures()
    {
        var user = new AppUser("Ali", "Gate Guard", UserRole.GateGuard, "#0EA5E9", "hash", 0);

        for (var i = 0; i < 15; i++)
            user.RecordFailedPin(Now);

        user.LockoutUntil.Should().Be(Now.AddMinutes(30));
    }

    [Fact]
    public void Owner_app_roles_need_six_digit_pins()
    {
        UserRoles.PinLengthFor(UserRole.Owner).Should().Be(6);
        UserRoles.PinLengthFor(UserRole.Finance).Should().Be(6);
        UserRoles.PinLengthFor(UserRole.GateGuard).Should().Be(4);
    }
}

public class SkuRulesTests
{
    [Theory]
    [InlineData("standard", 35, 31, 4)]
    [InlineData("standard", 70, 63, 7)]
    [InlineData("half", 35, 17, 18)]
    [InlineData("allA", 30, 30, 0)]
    public void Blends_round_C_half_up(string preset, int size, int expectedA, int expectedC) =>
        SkuRules.Blend(preset, size).Should().Be((expectedA, expectedC));

    [Fact]
    public void Prefix_uses_word_initials()
    {
        SkuRules.SuggestPrefix("Carhartt Detroit Pants", "Pants", new HashSet<string>()).Should().Be("CDP");
    }

    [Fact]
    public void Prefix_clash_extends_the_last_word()
    {
        var taken = new HashSet<string> { "CDP" };

        SkuRules.SuggestPrefix("Carhartt Dickies Polo", "Polos", taken).Should().Be("CDPO");
    }

    [Fact]
    public void Codes_are_zero_padded_but_grow_past_99() =>
        (SkuRules.FormatCode("CDP", 7), SkuRules.FormatCode("CDP", 140)).Should().Be(("CDP07", "CDP140"));

    [Theory]
    [InlineData("Nike Track Jacket", "Jackets")]
    [InlineData("Nike Track Pants", "Pants")]
    [InlineData("TNF Fleece Regular", "Fleeces")]
    [InlineData("RL Polo T-Shirt", "Polos")]
    [InlineData("Mystery Box", "Mix")]
    public void Classifier_keeps_legacy_keyword_order(string item, string expected) =>
        SkuCategoryClassifier.Classify(item).Should().Be(expected);
}
