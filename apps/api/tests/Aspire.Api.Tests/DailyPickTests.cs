using Aspire.Domain;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The server's half of the daily pick. It has to agree with
/// `dreams/board.test.ts`, because the nudge names a dream and then the
/// board opens on the same one.
/// </summary>
public sealed class DailyPickTests
{
    private const int Prague = 120;
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 5, 0, 0, TimeSpan.Zero);

    private static Dream Dream(
        string title,
        DateTimeOffset? shown = null,
        DreamStatus status = DreamStatus.Dreaming,
        int likes = 0,
        int order = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoardId = "board",
            Title = title,
            Status = status,
            LastShownAt = shown,
            Likes = likes,
            SortOrder = order
        };

    [Fact]
    public void An_empty_board_has_nothing_to_say()
    {
        Assert.Null(DailyPick.From([], Now, Prague));
    }

    [Fact]
    public void A_board_with_everything_achieved_has_nothing_to_say()
    {
        var rows = new[] { Dream("done", status: DreamStatus.Achieved) };

        Assert.Null(DailyPick.From(rows, Now, Prague));
    }

    [Fact]
    public void The_dream_already_shown_today_holds_the_day()
    {
        var today = new DateTimeOffset(2026, 9, 10, 4, 0, 0, TimeSpan.Zero);
        var rows = new[] { Dream("never"), Dream("today", today) };

        Assert.Equal("today", DailyPick.From(rows, Now, Prague)!.Title);
    }

    [Fact]
    public void One_never_shown_comes_before_one_shown_a_week_ago()
    {
        var rows = new[] { Dream("week", Now.AddDays(-7)), Dream("never") };

        Assert.Equal("never", DailyPick.From(rows, Now, Prague, new Random(1))!.Title);
    }

    [Fact]
    public void Once_every_dream_has_had_a_turn_the_one_with_the_most_fuel_is_next()
    {
        var rows = new[] { Dream("yesterday", Now.AddDays(-1)), Dream("week", Now.AddDays(-7)) };

        Assert.Equal("week", DailyPick.From(rows, Now, Prague)!.Title);
    }

    // ── the heart's one job (D58) ───────────────────────────────────────────

    [Fact]
    public void Ten_hearts_halve_the_wait_and_the_eleventh_does_nothing()
    {
        // Ten days loved equals twenty unloved, and a tie falls to board
        // order — which is the unloved one, first in the list.
        var waited = Dream("waited", Now.AddDays(-20), order: 0);
        var loved = Dream("loved", Now.AddDays(-10), likes: 10, order: 1);

        Assert.Equal("waited", DailyPick.From([waited, loved], Now, Prague)!.Title);
        Assert.Equal("loved", DailyPick.From([Dream("waited", Now.AddDays(-7)), loved], Now, Prague)!.Title);

        // Past the cap the number keeps going up and the pick stops listening.
        var adored = Dream("loved", Now.AddDays(-10), likes: 500, order: 1);
        Assert.Equal("waited", DailyPick.From([waited, adored], Now, Prague)!.Title);
    }

    [Fact]
    public void A_dream_nobody_has_seen_still_comes_before_the_most_loved_one()
    {
        var rows = new[] { Dream("loved", Now.AddDays(-10), likes: 10), Dream("never") };

        Assert.Equal("never", DailyPick.From(rows, Now, Prague, new Random(1))!.Title);
    }

    [Fact]
    public void The_day_holds_even_when_another_dream_has_more_fuel()
    {
        var rows = new[]
        {
            Dream("today", Now.AddHours(-1)),
            Dream("loved", Now.AddDays(-20), likes: 10)
        };

        Assert.Equal("today", DailyPick.From(rows, Now, Prague)!.Title);
    }

    [Theory]
    // Seven days, no hearts: the days themselves.
    [InlineData(7, 0, 7.0)]
    // One heart is a tenth more, ten are double, and above ten nothing moves.
    [InlineData(7, 1, 7.7)]
    [InlineData(7, 10, 14.0)]
    [InlineData(7, 99, 14.0)]
    // Shown today: no days waited, so no fuel whatever the hearts say.
    [InlineData(0, 10, 0.0)]
    public void Fuel_is_the_days_waited_times_what_the_hearts_add(int days, int likes, double expected)
    {
        var dream = Dream("d", Now.AddDays(-days), likes: likes);

        Assert.Equal(expected, DailyPick.FuelOf(dream, Now, Prague), 6);
    }

    [Fact]
    public void A_dream_nobody_has_seen_is_the_most_overdue_there_is()
    {
        Assert.Equal(double.PositiveInfinity, DailyPick.FuelOf(Dream("never"), Now, Prague));
    }

    [Fact]
    public void Fuel_counts_whole_days_so_two_devices_cannot_disagree()
    {
        // The same local day, half a minute apart. Hours elapsed would differ;
        // whole days cannot, and the phone works this out for itself (D36).
        var dream = Dream("d", Now.AddDays(-7));
        var later = Now.AddSeconds(30);

        Assert.Equal(DailyPick.FuelOf(dream, Now, Prague), DailyPick.FuelOf(dream, later, Prague));
        Assert.Equal(7.0, DailyPick.FuelOf(dream, later, Prague), 6);
    }

    [Fact]
    public void An_achieved_dream_is_never_picked_however_long_it_has_been()
    {
        var rows = new[]
        {
            Dream("done", Now.AddYears(-1), DreamStatus.Achieved),
            Dream("yesterday", Now.AddDays(-1))
        };

        Assert.Equal("yesterday", DailyPick.From(rows, Now, Prague)!.Title);
    }

    [Fact]
    public void The_day_is_the_devices_own_and_not_utc()
    {
        // Stamped at 22:30 UTC on the 9th, which in Prague was half past
        // midnight on the 10th — the same local day the pick is being made on
        // (07:00 there). Read as a UTC date it would be the 9th, the pick
        // would not hold, and the board would move under the person.
        var lateLastNight = new DateTimeOffset(2026, 9, 9, 22, 30, 0, TimeSpan.Zero);
        var rows = new[] { Dream("never"), Dream("last-night", lateLastNight) };

        Assert.Equal("last-night", DailyPick.From(rows, Now, Prague)!.Title);

        // Seven hours behind, the same instant is still the evening of the
        // 9th and so is the stamp — also one local day, and also held. The
        // rule is the device's day either way, never the server's.
        Assert.Equal("last-night", DailyPick.From(rows, Now, -7 * 60)!.Title);

        // Two days earlier is nobody's today, and the pick moves on.
        var older = new[] { Dream("never"), Dream("older", Now.AddDays(-2)) };
        Assert.Equal("never", DailyPick.From(older, Now, Prague, new Random(1))!.Title);
    }
}
