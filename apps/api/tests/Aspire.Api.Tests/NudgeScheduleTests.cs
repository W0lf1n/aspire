using Aspire.Domain;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class NudgeScheduleTests
{
    private const int SevenAm = 7 * 60;

    /// <summary>Thursday 10 September 2026, as a local wall clock.</summary>
    private static DateTime Thursday(int hour, int minute = 0) => new(2026, 9, 10, hour, minute, 0);

    private static DateTime Saturday(int hour) => new(2026, 9, 12, hour, 0, 0);

    [Fact]
    public void Off_is_never_due()
    {
        Assert.False(NudgeSchedule.IsDue(NudgeMode.Off, SevenAm, Thursday(7), null));
    }

    [Fact]
    public void Daily_is_due_at_the_time_and_not_before()
    {
        Assert.False(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Thursday(6, 59), null));
        Assert.True(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Thursday(7), null));
    }

    [Fact]
    public void A_worker_that_was_stopped_over_breakfast_still_sends()
    {
        Assert.True(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Thursday(8, 30), null));
    }

    [Fact]
    public void A_nudge_that_missed_the_morning_is_not_sent_at_bedtime()
    {
        // Past the grace window the day is skipped: a dream at eleven at night
        // is not the morning habit, and would be the first unwelcome one.
        Assert.False(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Thursday(23), null));
        Assert.False(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Thursday(9, 1), null));
    }

    [Fact]
    public void A_day_gets_one()
    {
        var today = new DateOnly(2026, 9, 10);

        Assert.False(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Thursday(7, 30), today));
        // Yesterday's stamp does not stop today's.
        Assert.True(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Thursday(7, 30), today.AddDays(-1)));
    }

    [Fact]
    public void Weekdays_skips_the_weekend_and_daily_does_not()
    {
        Assert.False(NudgeSchedule.IsDue(NudgeMode.Weekdays, SevenAm, Saturday(7), null));
        Assert.True(NudgeSchedule.IsDue(NudgeMode.Daily, SevenAm, Saturday(7), null));
        Assert.True(NudgeSchedule.IsDue(NudgeMode.Weekdays, SevenAm, Thursday(7), null));
    }

    [Fact]
    public void The_local_clock_comes_from_the_offset()
    {
        var utc = new DateTimeOffset(2026, 9, 10, 5, 0, 0, TimeSpan.Zero);

        // Prague in summer is two hours ahead: seven in the morning there.
        Assert.Equal(new DateTime(2026, 9, 10, 7, 0, 0), NudgeSchedule.LocalNow(utc, 120));
        // And somewhere behind it is still the night before.
        Assert.Equal(new DateTime(2026, 9, 9, 22, 0, 0), NudgeSchedule.LocalNow(utc, -7 * 60));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(7 * 60, true)]
    [InlineData(24 * 60 - 1, true)]
    [InlineData(-1, false)]
    [InlineData(24 * 60, false)]
    public void A_time_of_day_is_a_minute_of_one(int minutes, bool ok)
    {
        Assert.Equal(ok, NudgeSchedule.IsTimeOfDay(minutes));
    }

    [Theory]
    [InlineData(120, true)]
    [InlineData(14 * 60, true)]
    [InlineData(-12 * 60, true)]
    [InlineData(15 * 60, false)]
    [InlineData(-13 * 60, false)]
    public void An_offset_is_one_a_real_place_has(int minutes, bool ok)
    {
        Assert.Equal(ok, NudgeSchedule.IsUtcOffset(minutes));
    }
}
