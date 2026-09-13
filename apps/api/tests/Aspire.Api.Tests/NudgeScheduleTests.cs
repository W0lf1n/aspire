using Aspire.Domain;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class NudgeScheduleTests
{
    private const int SevenAm = 7 * 60;
    private const int Noon = 12 * 60;
    private const int SixPm = 18 * 60;

    private static readonly int[] Seven = [SevenAm];

    /// <summary>Thursday 10 September 2026, as a local wall clock.</summary>
    private static DateTime Thursday(int hour, int minute = 0) => new(2026, 9, 10, hour, minute, 0);

    private static DateTime Saturday(int hour) => new(2026, 9, 12, hour, 0, 0);

    private static readonly DateOnly Today = new(2026, 9, 10);

    [Fact]
    public void Off_is_never_due()
    {
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Off, Seven, Thursday(7), null, null));
    }

    [Fact]
    public void Daily_is_due_at_the_time_and_not_before()
    {
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Thursday(6, 59), null, null));
        Assert.Equal(SevenAm, NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Thursday(7), null, null));
    }

    [Fact]
    public void A_worker_that_was_stopped_over_breakfast_still_sends()
    {
        Assert.Equal(SevenAm, NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Thursday(8, 30), null, null));
    }

    [Fact]
    public void A_nudge_that_missed_the_morning_is_not_sent_at_bedtime()
    {
        // Past the grace window the reminder is skipped: a dream at eleven at
        // night is not the habit this is, and would be the first unwelcome one.
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Thursday(23), null, null));
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Thursday(9, 1), null, null));
    }

    [Fact]
    public void One_reminder_is_sent_once()
    {
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Thursday(7, 30), Today, SevenAm));
        // Yesterday's stamp does not stop today's.
        Assert.Equal(
            SevenAm,
            NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Thursday(7, 30), Today.AddDays(-1), SevenAm));
    }

    [Fact]
    public void Weekdays_skips_the_weekend_and_daily_does_not()
    {
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Weekdays, Seven, Saturday(7), null, null));
        Assert.Equal(SevenAm, NudgeSchedule.DueAt(NudgeMode.Daily, Seven, Saturday(7), null, null));
        Assert.Equal(SevenAm, NudgeSchedule.DueAt(NudgeMode.Weekdays, Seven, Thursday(7), null, null));
    }

    // ── several a day (D72) ────────────────────────────────────────────────

    private static readonly int[] Three = [SevenAm, Noon, SixPm];

    [Fact]
    public void Each_reminder_comes_round_in_its_turn()
    {
        // Morning: the first, and nothing has been sent yet.
        Assert.Equal(SevenAm, NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(7), null, null));

        // Sent. Nothing more until noon.
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(7, 30), Today, SevenAm));
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(11, 59), Today, SevenAm));

        Assert.Equal(Noon, NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(12), Today, SevenAm));
        Assert.Equal(SixPm, NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(18), Today, Noon));

        // The last one done, and the day is over.
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(18, 30), Today, SixPm));
    }

    [Fact]
    public void Tomorrow_starts_the_list_again()
    {
        // Yesterday got all three; this morning is owed the first.
        Assert.Equal(
            SevenAm,
            NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(7), Today.AddDays(-1), SixPm));
    }

    [Fact]
    public void A_server_that_was_down_sends_the_most_recent_and_not_both()
    {
        // Reminders at 07:00 and 08:00, and the worker comes back at 08:30
        // with both inside their grace. The point is a dream now, so it is the
        // later one — and marking it sent takes the one it overtook with it.
        int[] twice = [SevenAm, SevenAm + 60];

        var owed = NudgeSchedule.DueAt(NudgeMode.Daily, twice, Thursday(8, 30), null, null);

        Assert.Equal(SevenAm + 60, owed);
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, twice, Thursday(8, 35), Today, owed));
    }

    [Fact]
    public void A_reminder_added_earlier_than_one_already_sent_today_waits_for_tomorrow()
    {
        // Seven has been sent, and noon is added at nine. Noon still comes,
        // because it is after the stamp.
        Assert.Equal(Noon, NudgeSchedule.DueAt(NudgeMode.Daily, Three, Thursday(12), Today, SevenAm));

        // An hour added *before* the stamp does not fire retroactively: the
        // list is „everything up to here is done“, and six this morning is
        // behind seven.
        int[] withSix = [6 * 60, SevenAm];
        Assert.Null(NudgeSchedule.DueAt(NudgeMode.Daily, withSix, Thursday(7, 30), Today, SevenAm));
    }

    [Theory]
    [InlineData(new[] { 420 }, true)]
    [InlineData(new[] { 0, 60, 120, 180, 240 }, true)]
    [InlineData(new int[0], false)]
    [InlineData(new[] { 0, 60, 120, 180, 240, 300 }, false)]
    [InlineData(new[] { 420, 420 }, false)]
    [InlineData(new[] { 420, 24 * 60 }, false)]
    [InlineData(new[] { -1 }, false)]
    public void A_set_of_times_is_one_to_five_distinct_times_of_day(int[] times, bool ok)
    {
        Assert.Equal(ok, NudgeSchedule.AreTimesOfDay(times));
    }

    [Fact]
    public void Nothing_at_all_is_not_a_set_of_times()
    {
        Assert.False(NudgeSchedule.AreTimesOfDay(null));
    }

    [Fact]
    public void Tidy_sorts_and_drops_repeats()
    {
        Assert.Equal([SevenAm, Noon, SixPm], NudgeSchedule.Tidy([SixPm, SevenAm, Noon, SevenAm]));
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
