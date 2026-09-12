using Aspire.Domain;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// A mirror of `board.test.ts`'s `anniversaryToday`, case for case, because
/// the two are the same rule in two languages and a morning where they
/// disagree is a notification about one dream and a board showing another
/// (D57).
/// </summary>
public sealed class AnniversaryTests
{
    /// <summary>Ten in the morning on 10 September 2026, in Prague.</summary>
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
    private const int Prague = 120;

    /// <summary>Achieved on 10 September, the day <see cref="Now"/> falls on.</summary>
    private static Dream Achieved(string title, int yearsAgo, int month = 9, int day = 10, int order = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            BoardId = "board",
            Title = title,
            Status = DreamStatus.Achieved,
            SortOrder = order,
            AchievedAt = new DateTimeOffset(2026 - yearsAgo, month, day, 12, 0, 0, TimeSpan.Zero)
        };

    [Fact]
    public void Finds_the_dream_that_came_true_on_this_day_in_an_earlier_year()
    {
        var found = Anniversary.Today([Achieved("Loď", 1)], Now, Prague);

        Assert.NotNull(found);
        Assert.Equal("Loď", found!.Value.Dream.Title);
        Assert.Equal(1, found.Value.Years);
    }

    [Fact]
    public void Counts_the_whole_years()
    {
        Assert.Equal(4, Anniversary.Today([Achieved("Loď", 4)], Now, Prague)!.Value.Years);
    }

    [Fact]
    public void Is_nothing_on_any_other_day_and_nothing_on_the_day_itself()
    {
        Assert.Null(Anniversary.Today([Achieved("Loď", 1, day: 11)], Now, Prague));
        Assert.Null(Anniversary.Today([Achieved("Loď", 1, month: 10)], Now, Prague));
        // Achieved this morning is not an anniversary; it is today.
        Assert.Null(Anniversary.Today([Achieved("Loď", 0)], Now, Prague));
    }

    [Fact]
    public void Ignores_a_dream_that_is_not_achieved()
    {
        var still = Achieved("Loď", 1);
        still.Status = DreamStatus.InProgress;

        Assert.Null(Anniversary.Today([still], Now, Prague));
        Assert.Null(Anniversary.Today([], Now, Prague));
    }

    [Fact]
    public void Takes_the_most_recent_when_two_fell_on_the_same_day()
    {
        var found = Anniversary.Today([Achieved("starý", 5), Achieved("nedávný", 2)], Now, Prague);

        Assert.Equal("nedávný", found!.Value.Dream.Title);
        Assert.Equal(2, found.Value.Years);
    }

    [Fact]
    public void The_day_is_the_devices_own()
    {
        // 23:30 UTC on 9 September is half past one on the 10th in Prague, and
        // the dream was achieved at noon on the 10th four years ago. The
        // anniversary belongs to the day the person is living in.
        var lateEvening = new DateTimeOffset(2026, 9, 9, 23, 30, 0, TimeSpan.Zero);
        var dreams = new[] { Achieved("Loď", 4) };

        Assert.NotNull(Anniversary.Today(dreams, lateEvening, Prague));
        Assert.Null(Anniversary.Today(dreams, lateEvening, 0));
    }

    [Fact]
    public void A_dream_achieved_on_29_February_has_its_anniversary_on_29_February()
    {
        var leapling = Achieved("Loď", 2, month: 2, day: 29);

        var onTheDay = new DateTimeOffset(2028, 2, 29, 8, 0, 0, TimeSpan.Zero);
        var theYearWithout = new DateTimeOffset(2027, 2, 28, 8, 0, 0, TimeSpan.Zero);
        var theFirstOfMarch = new DateTimeOffset(2027, 3, 1, 8, 0, 0, TimeSpan.Zero);

        Assert.Equal(4, Anniversary.Today([leapling], onTheDay, Prague)!.Value.Years);
        Assert.Null(Anniversary.Today([leapling], theYearWithout, Prague));
        Assert.Null(Anniversary.Today([leapling], theFirstOfMarch, Prague));
    }
}
