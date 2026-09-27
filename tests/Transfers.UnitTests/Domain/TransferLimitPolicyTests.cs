using Transfers.Domain.Limits;

namespace Transfers.UnitTests.Domain;

public sealed class TransferLimitPolicyTests
{
    private static readonly TransferLimitPolicy Policy = new(
        TestData.SaoPaulo,
        dayStartsAt: new TimeOnly(6, 0),
        nightStartsAt: new TimeOnly(22, 0),
        TestData.DayLimit,
        TestData.NightLimit);

    [Theory]
    [InlineData("06:00", DayPeriod.Day)]
    [InlineData("12:00", DayPeriod.Day)]
    [InlineData("21:59:59", DayPeriod.Day)]
    [InlineData("22:00", DayPeriod.Night)]
    [InlineData("23:30", DayPeriod.Night)]
    [InlineData("00:00", DayPeriod.Night)]
    [InlineData("05:59:59", DayPeriod.Night)]
    public void GetPeriodAt_UsesLocalTimeOfTheConfiguredTimeZone(string localTime, DayPeriod expected)
    {
        var instant = AtLocalTime(TimeOnly.Parse(localTime, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(expected, Policy.GetPeriodAt(instant));
    }

    [Fact]
    public void GetPeriodAt_ConvertsFromUtc()
    {
        // 01:00 UTC = 22:00 do dia anterior em São Paulo (UTC-3): noite.
        var instant = new DateTimeOffset(2026, 1, 16, 1, 0, 0, TimeSpan.Zero);

        Assert.Equal(DayPeriod.Night, Policy.GetPeriodAt(instant));
    }

    [Fact]
    public void GetLimitAt_ReturnsDayOrNightLimit()
    {
        Assert.Same(TestData.DayLimit, Policy.GetLimitAt(AtLocalTime(new TimeOnly(10, 0))));
        Assert.Same(TestData.NightLimit, Policy.GetLimitAt(AtLocalTime(new TimeOnly(23, 0))));
    }

    [Fact]
    public void WindowStart_IsOneHourBefore()
    {
        Assert.Equal(TestData.Now.AddHours(-1), TransferLimitPolicy.WindowStart(TestData.Now));
    }

    [Fact]
    public void Constructor_WithDayStartingAfterNight_Throws()
    {
        Assert.Throws<ArgumentException>(() => new TransferLimitPolicy(
            TestData.SaoPaulo, new TimeOnly(22, 0), new TimeOnly(6, 0), TestData.DayLimit, TestData.NightLimit));
    }

    [Fact]
    public void PeriodLimit_WithNonPositiveValues_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PeriodLimit(0m, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PeriodLimit(100m, 0));
    }

    private static DateTimeOffset AtLocalTime(TimeOnly time) =>
        new(new DateOnly(2026, 1, 15).ToDateTime(time), TimeSpan.FromHours(-3));
}
