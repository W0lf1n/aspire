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

    private static Dream Dream(string title, DateTimeOffset? shown = null, DreamStatus status = DreamStatus.Dreaming) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoardId = "board",
            Title = title,
            Status = status,
            LastShownAt = shown
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
    public void Once_every_dream_has_had_a_turn_the_oldest_is_next()
    {
        var rows = new[] { Dream("yesterday", Now.AddDays(-1)), Dream("week", Now.AddDays(-7)) };

        Assert.Equal("week", DailyPick.From(rows, Now, Prague)!.Title);
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
