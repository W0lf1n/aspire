using Aspire.Domain;
using Aspire.Infrastructure.Media;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The six a lock screen is made of when nobody chose them. A mirror of
/// `wallpaper.test.ts`'s `wallpaperPick`, because the phone shows the same six
/// on Tapeta before the automation ever fetches them, and two different
/// answers would be two different wallpapers (D59).
/// </summary>
public sealed class WallpaperPickTests
{
    private const int Prague = 120;
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 5, 0, 0, TimeSpan.Zero);

    private static Dream Dream(
        string title,
        DateTimeOffset? shown = null,
        int likes = 0,
        int order = 0,
        DreamStatus status = DreamStatus.Dreaming) =>
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

    /// <summary>Every dream given to it has a photograph, unless a test says otherwise.</summary>
    private static List<Dream> Pick(IReadOnlyList<Dream> dreams, Guid? picked = null, int count = 6) =>
        WallpaperPick.From(dreams, dreams.Select(d => d.Id).ToHashSet(), picked, Now, Prague, count);

    [Fact]
    public void The_day_is_first_and_the_rest_are_in_fuel_order()
    {
        var week = Dream("week", Now.AddDays(-7), order: 0);
        var today = Dream("today", Now.AddHours(-2), order: 1);
        var loved = Dream("loved", Now.AddDays(-7), likes: 10, order: 2);

        var chosen = Pick([week, today, loved], today.Id, 3);

        Assert.Equal(["today", "loved", "week"], chosen.Select(d => d.Title));
    }

    [Fact]
    public void A_dream_nobody_has_seen_comes_before_any_that_has()
    {
        var loved = Dream("loved", Now.AddDays(-10), likes: 10, order: 0);
        var never = Dream("never", order: 1);

        Assert.Equal(["never", "loved"], Pick([loved, never]).Select(d => d.Title));
    }

    [Fact]
    public void Two_nobody_has_seen_fall_to_board_order()
    {
        var second = Dream("second", order: 2);
        var first = Dream("first", order: 1);

        Assert.Equal(["first", "second"], Pick([second, first]).Select(d => d.Title));
    }

    [Fact]
    public void The_wall_is_left_out_and_so_is_a_dream_with_no_photograph()
    {
        var ahead = Dream("ahead", Now.AddDays(-7));
        var done = Dream("done", Now.AddDays(-7), status: DreamStatus.Achieved);
        var bare = Dream("bare", Now.AddDays(-20));

        // `bare` is not in the set of dreams that have a photograph.
        var chosen = WallpaperPick.From(
            [ahead, done, bare],
            new HashSet<Guid> { ahead.Id, done.Id },
            null,
            Now,
            Prague,
            6);

        Assert.Equal(["ahead"], chosen.Select(d => d.Title));
    }

    [Fact]
    public void It_never_hands_back_more_than_the_collage_holds()
    {
        var rows = Enumerable.Range(0, 9)
            .Select(i => Dream($"d{i}", Now.AddDays(-i - 1), order: i))
            .ToList();

        Assert.Equal(CollageLayout.MaxPhotographs, Pick(rows, count: CollageLayout.MaxPhotographs).Count);
        Assert.Empty(Pick(rows, count: 0));
    }

    [Fact]
    public void A_board_with_nothing_ahead_of_it_makes_no_wallpaper()
    {
        Assert.Empty(Pick([]));
        Assert.Empty(Pick([Dream("done", Now.AddDays(-1), status: DreamStatus.Achieved)]));
    }

    [Fact]
    public void A_pick_that_cannot_be_on_one_is_simply_not_first()
    {
        var ahead = Dream("ahead", Now.AddDays(-7));
        var bare = Dream("bare", Now.AddDays(-20));

        var chosen = WallpaperPick.From(
            [ahead, bare],
            new HashSet<Guid> { ahead.Id },
            bare.Id,
            Now,
            Prague,
            6);

        Assert.Equal(["ahead"], chosen.Select(d => d.Title));
    }
}
