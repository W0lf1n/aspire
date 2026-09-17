using Aspire.Api.Dreams;
using Aspire.Domain;
using Aspire.Infrastructure;
using Aspire.Infrastructure.Media;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class DreamServiceTests : IDisposable
{
    private const string BoardA = "board-a";
    private const string BoardB = "board-b";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly DreamService _dreams;

    public DreamServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        _db.Boards.Add(new Board { Id = BoardA, Name = "A" });
        _db.Boards.Add(new Board { Id = BoardB, Name = "B" });
        _db.SaveChanges();
        _dreams = new DreamService(_db, new MediaStore(Path.Combine(Path.GetTempPath(), $"aspire-media-{Guid.NewGuid():N}")));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static DreamInput Input(string title = "Dům u lesa", string? why = "Ticho a les za oknem.",
        DreamStatus? status = DreamStatus.Dreaming, int? year = 2030, string? affirmation = null,
        DreamCategory? category = null) =>
        new(title, why, status, category, year, affirmation);

    [Fact]
    public async Task A_new_dream_goes_in_front_of_its_own_board()
    {
        // Number one in the Seznam is the dream just written (D44, D79), and
        // another board's dreams do not count towards where that is.
        var first = await _dreams.CreateAsync(BoardA, Input("První"));
        await _dreams.CreateAsync(BoardB, Input("Cizí"));
        var second = await _dreams.CreateAsync(BoardA, Input("Druhý"));

        Assert.Equal(first.SortOrder - 1, second.SortOrder);
        var listed = await _dreams.ListAsync(BoardA);
        Assert.Equal(["Druhý", "První"], listed.Select(d => d.Title));
    }

    [Fact]
    public async Task Creating_trims_and_defaults()
    {
        var dream = await _dreams.CreateAsync(BoardA, new DreamInput("  Loď  ", null, null, null, null, null));

        Assert.Equal("Loď", dream.Title);
        Assert.Equal(string.Empty, dream.Why);
        Assert.Equal(string.Empty, dream.Affirmation);
        Assert.Equal(DreamStatus.Dreaming, dream.Status);
        Assert.Null(dream.TargetYear);
        Assert.Null(dream.Category);
        Assert.Equal(0, dream.Likes);
        Assert.Null(dream.AchievedAt);
    }

    [Theory]
    [InlineData("", "Ticho.", 2030, "Napiš název.")]
    [InlineData("   ", "Ticho.", 2030, "Napiš název.")]
    [InlineData("x", "Ticho.", 1999, "Rok napiš mezi 2000 a 2100.")]
    [InlineData("x", "Ticho.", 2101, "Rok napiš mezi 2000 a 2100.")]
    public void A_bad_input_earns_a_sentence(string title, string why, int? year, string expected)
    {
        Assert.Equal(
            expected,
            DreamService.Problem(new DreamInput(title, why, DreamStatus.Dreaming, null, year, null)));
    }

    [Fact]
    public void Lengths_are_the_columns()
    {
        Assert.Null(DreamService.Problem(Input(new string('a', Dream.TitleMaxLength), new string('b', Dream.WhyMaxLength))));
        Assert.Equal("Název má nejvýš 120 znaků.", DreamService.Problem(Input(new string('a', Dream.TitleMaxLength + 1))));
        Assert.Equal("Proč má nejvýš 500 znaků.", DreamService.Problem(Input("x", new string('b', Dream.WhyMaxLength + 1))));
        Assert.Null(DreamService.Problem(Input("x", affirmation: new string('c', Dream.AffirmationMaxLength))));
        Assert.Equal(
            "Afirmace má nejvýš 120 znaků.",
            DreamService.Problem(Input("x", affirmation: new string('c', Dream.AffirmationMaxLength + 1))));
        Assert.Null(DreamService.Problem(Input("x", null, null, null)));
    }

    [Fact]
    public async Task A_category_is_kept_and_can_be_taken_back_off()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input(category: DreamCategory.Want));
        Assert.Equal(DreamCategory.Want, dream.Category);

        var moved = await _dreams.UpdateAsync(BoardA, dream.Id, Input(category: DreamCategory.Do));
        Assert.Equal(DreamCategory.Do, moved!.Category);

        var none = await _dreams.UpdateAsync(BoardA, dream.Id, Input(category: null));
        Assert.Null(none!.Category);
    }

    [Fact]
    public void Every_category_survives_the_round_trip_through_its_column()
    {
        foreach (var category in Enum.GetValues<DreamCategory>())
        {
            Assert.Equal(category, DreamCategoryNames.Parse(DreamCategoryNames.ToWire(category)));
        }

        // The three of PLAN.md §3.1, and no fourth without a decision (D43).
        Assert.Equal(3, Enum.GetValues<DreamCategory>().Length);
    }

    [Fact]
    public async Task The_affirmation_is_trimmed_and_kept()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input(affirmation: "  Bydlím u lesa.  "));
        Assert.Equal("Bydlím u lesa.", dream.Affirmation);

        // Cleared the way any other field is: by being sent empty.
        var cleared = await _dreams.UpdateAsync(BoardA, dream.Id, Input(affirmation: "   "));
        Assert.Equal(string.Empty, cleared!.Affirmation);
    }

    [Fact]
    public async Task Achieved_gets_its_date_once_and_gives_it_back_when_it_leaves()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input());

        var achieved = await _dreams.UpdateAsync(BoardA, dream.Id, Input(status: DreamStatus.Achieved));
        var when = achieved!.AchievedAt;
        Assert.NotNull(when);

        var still = await _dreams.UpdateAsync(BoardA, dream.Id, Input("Jiný název", status: DreamStatus.Achieved));
        Assert.Equal(when, still!.AchievedAt);
        Assert.Equal("Jiný název", still.Title);

        var back = await _dreams.UpdateAsync(BoardA, dream.Id, Input(status: DreamStatus.InProgress));
        Assert.Null(back!.AchievedAt);
        Assert.Equal(DreamStatus.InProgress, back.Status);
    }

    [Fact]
    public async Task A_like_is_one_more_every_time()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input());

        await _dreams.LikeAsync(BoardA, dream.Id);
        var liked = await _dreams.LikeAsync(BoardA, dream.Id);

        Assert.Equal(2, liked!.Likes);
    }

    [Fact]
    public async Task Being_shown_stamps_the_dream_and_nothing_else()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input());
        var other = await _dreams.CreateAsync(BoardA, Input("Loď"));
        var before = DateTimeOffset.UtcNow;

        Assert.True(await _dreams.MarkShownAsync(BoardA, dream.Id));

        // Read past the change tracker: the stamp is an ExecuteUpdate, so the
        // instance `CreateAsync` left tracked still says null.
        var shown = await _db.Dreams.AsNoTracking().FirstAsync(d => d.Id == dream.Id);
        Assert.NotNull(shown.LastShownAt);
        Assert.InRange(shown.LastShownAt!.Value, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.Null((await _db.Dreams.AsNoTracking().FirstAsync(d => d.Id == other.Id)).LastShownAt);
    }

    [Fact]
    public async Task A_dream_that_is_not_there_was_not_shown()
    {
        Assert.False(await _dreams.MarkShownAsync(BoardA, Guid.NewGuid()));
    }

    [Fact]
    public async Task Deleting_removes_the_dream()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input());

        Assert.True(await _dreams.DeleteAsync(BoardA, dream.Id));
        Assert.Empty(await _dreams.ListAsync(BoardA));
        Assert.False(await _dreams.DeleteAsync(BoardA, dream.Id));
    }

    [Fact]
    public async Task Another_board_cannot_see_touch_or_like_it()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input());

        Assert.Empty(await _dreams.ListAsync(BoardB));
        Assert.Null(await _dreams.FindAsync(BoardB, dream.Id));
        Assert.Null(await _dreams.UpdateAsync(BoardB, dream.Id, Input("Ukradený")));
        Assert.Null(await _dreams.LikeAsync(BoardB, dream.Id));
        Assert.False(await _dreams.MarkShownAsync(BoardB, dream.Id));
        Assert.False(await _dreams.DeleteAsync(BoardB, dream.Id));

        var untouched = await _dreams.FindAsync(BoardA, dream.Id);
        Assert.Equal("Dům u lesa", untouched!.Title);
        Assert.Equal(0, untouched.Likes);
        Assert.Null(untouched.LastShownAt);
    }

    // ── Teď, the second reel (D53) ──────────────────────────────────────────

    /// <summary>Some dreams on a board, in the order they were made.</summary>
    private async Task<List<Dream>> Made(int count, string board = BoardA)
    {
        var rows = new List<Dream>();
        for (var i = 0; i < count; i++) rows.Add(await _dreams.CreateAsync(board, Input($"Sen {i}")));
        return rows;
    }

    private async Task<List<string>> FocusTitles(string board = BoardA) =>
        (await _dreams.FocusAsync(board)).Select(d => d.Title).ToList();

    [Fact]
    public async Task Teď_is_empty_until_something_is_put_on_it()
    {
        await Made(3);

        Assert.Empty(await _dreams.FocusAsync(BoardA));
    }

    [Fact]
    public async Task A_dream_joins_Teď_behind_the_ones_already_there()
    {
        var made = await Made(3);

        foreach (var dream in made) await _dreams.AddToFocusAsync(BoardA, dream.Id);

        Assert.Equal(["Sen 0", "Sen 1", "Sen 2"], await FocusTitles());
    }

    [Fact]
    public async Task Putting_the_same_dream_on_twice_changes_nothing()
    {
        // Two devices tapping the same pill must not make an error, and must
        // not put the dream on Teď twice.
        var made = await Made(2);
        await _dreams.AddToFocusAsync(BoardA, made[0].Id);
        await _dreams.AddToFocusAsync(BoardA, made[1].Id);

        var (again, problem) = await _dreams.AddToFocusAsync(BoardA, made[0].Id);

        Assert.Null(problem);
        Assert.Equal(1, again!.FocusRank);
        Assert.Equal(["Sen 0", "Sen 1"], await FocusTitles());
    }

    [Fact]
    public async Task The_eleventh_dream_is_refused_with_a_sentence()
    {
        var made = await Made(Dream.FocusMax + 1);
        for (var i = 0; i < Dream.FocusMax; i++) await _dreams.AddToFocusAsync(BoardA, made[i].Id);

        var (dream, problem) = await _dreams.AddToFocusAsync(BoardA, made[^1].Id);

        Assert.Null(dream);
        Assert.Equal("Na teď máš už 10 snů. Některý nejdřív odeber.", problem);
        Assert.Equal(Dream.FocusMax, (await _dreams.FocusAsync(BoardA)).Count);
    }

    [Fact]
    public async Task An_achieved_dream_does_not_belong_on_Teď()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input(status: DreamStatus.Achieved));

        var (added, problem) = await _dreams.AddToFocusAsync(BoardA, dream.Id);

        Assert.Null(added);
        Assert.Equal("Splněný sen na teď nepatří.", problem);
    }

    [Fact]
    public async Task Marking_a_dream_splněno_takes_it_off_Teď()
    {
        // It leaves Teď the way it leaves the reel: it is behind you now.
        var made = await Made(2);
        foreach (var dream in made) await _dreams.AddToFocusAsync(BoardA, dream.Id);

        await _dreams.UpdateAsync(BoardA, made[0].Id, Input("Sen 0", status: DreamStatus.Achieved));

        Assert.Equal(["Sen 1"], await FocusTitles());
    }

    [Fact]
    public async Task Taking_a_dream_off_Teď_leaves_the_rest_in_order()
    {
        var made = await Made(3);
        foreach (var dream in made) await _dreams.AddToFocusAsync(BoardA, dream.Id);

        Assert.True(await _dreams.RemoveFromFocusAsync(BoardA, made[1].Id));

        // The gap the middle one left costs nothing: the screen numbers by
        // place in the list, not by the rank.
        Assert.Equal(["Sen 0", "Sen 2"], await FocusTitles());
    }

    [Fact]
    public async Task Taking_off_a_dream_that_is_not_on_it_is_not_a_failure()
    {
        var made = await Made(1);

        Assert.True(await _dreams.RemoveFromFocusAsync(BoardA, made[0].Id));
        Assert.False(await _dreams.RemoveFromFocusAsync(BoardA, Guid.NewGuid()));
    }

    [Fact]
    public async Task The_whole_order_is_written_in_one_go()
    {
        var made = await Made(4);
        foreach (var dream in made.Take(3)) await _dreams.AddToFocusAsync(BoardA, dream.Id);

        // Reversed, with a fourth joining and the first dropping out.
        var (rows, problem) = await _dreams.ReorderFocusAsync(
            BoardA, [made[3].Id, made[2].Id, made[1].Id]);

        Assert.Null(problem);
        Assert.NotNull(rows);
        Assert.Equal(["Sen 3", "Sen 2", "Sen 1"], rows.Select(d => d.Title));
        Assert.Equal([1, 2, 3], rows.Select(d => d.FocusRank));
        Assert.Equal(["Sen 3", "Sen 2", "Sen 1"], await FocusTitles());
    }

    [Fact]
    public async Task Two_dreams_swapping_places_do_not_collide_on_the_way_past()
    {
        // Why the index is not unique: a unique one is checked per statement,
        // and the first half of a swap would collide with the second.
        var made = await Made(2);
        foreach (var dream in made) await _dreams.AddToFocusAsync(BoardA, dream.Id);

        var (rows, problem) = await _dreams.ReorderFocusAsync(BoardA, [made[1].Id, made[0].Id]);

        Assert.Null(problem);
        Assert.Equal(["Sen 1", "Sen 0"], rows!.Select(d => d.Title));
    }

    [Fact]
    public async Task An_empty_order_empties_Teď()
    {
        var made = await Made(2);
        foreach (var dream in made) await _dreams.AddToFocusAsync(BoardA, dream.Id);

        var (rows, problem) = await _dreams.ReorderFocusAsync(BoardA, []);

        Assert.Null(problem);
        Assert.Empty(rows!);
        Assert.Empty(await _dreams.FocusAsync(BoardA));
    }

    [Fact]
    public async Task An_order_that_makes_no_sense_earns_a_sentence_and_changes_nothing()
    {
        var made = await Made(2);
        await _dreams.AddToFocusAsync(BoardA, made[0].Id);
        var elsewhere = await _dreams.CreateAsync(BoardB, Input("Cizí"));

        Assert.Equal(
            "Jeden sen tam nemůže být dvakrát.",
            (await _dreams.ReorderFocusAsync(BoardA, [made[1].Id, made[1].Id])).Problem);
        Assert.Equal(
            "Některý z těch snů na nástěnce není.",
            (await _dreams.ReorderFocusAsync(BoardA, [elsewhere.Id])).Problem);
        Assert.Equal(
            "Na teď se vejde nejvýš 10 snů.",
            (await _dreams.ReorderFocusAsync(
                BoardA, [.. (await Made(Dream.FocusMax + 1)).Select(d => d.Id)])).Problem);

        Assert.Equal(["Sen 0"], await FocusTitles());
    }

    [Fact]
    public async Task Another_board_cannot_reach_Teď()
    {
        var made = await Made(1);
        await _dreams.AddToFocusAsync(BoardA, made[0].Id);

        Assert.Equal((null, null), await _dreams.AddToFocusAsync(BoardB, made[0].Id));
        Assert.False(await _dreams.RemoveFromFocusAsync(BoardB, made[0].Id));
        Assert.Empty(await _dreams.FocusAsync(BoardB));
        Assert.Equal(["Sen 0"], await FocusTitles());
    }

    [Fact]
    public async Task A_place_on_Teď_is_not_a_change_to_the_dream()
    {
        // The date on a dream's screen means „I last changed this“ (D77):
        // which reel it is on, and where, is about the board.
        var made = await Made(2);
        var before = made.Select(d => d.UpdatedAt).ToList();

        await _dreams.AddToFocusAsync(BoardA, made[0].Id);
        await _dreams.ReorderFocusAsync(BoardA, [made[1].Id, made[0].Id]);
        await _dreams.RemoveFromFocusAsync(BoardA, made[1].Id);
        await _dreams.LikeAsync(BoardA, made[0].Id);

        await _dreams.PlaceAsync(BoardA, made[0].Id, 1);

        var after = (await _dreams.ListAsync(BoardA)).ToDictionary(d => d.Id, d => d.UpdatedAt);
        Assert.Equal(before, made.Select(d => after[d.Id]).ToList());
    }

    // ── the Seznam's own order (D79) ────────────────────────────────────────

    private async Task<List<string>> Titles() =>
        (await _dreams.ListAsync(BoardA)).Select(d => d.Title).ToList();

    [Fact]
    public async Task A_dream_moved_up_pushes_the_ones_it_passed_down_by_one()
    {
        // Made 0, 1, 2, 3 and each went in front, so the list reads 3, 2, 1, 0.
        var made = await Made(4);
        Assert.Equal(["Sen 3", "Sen 2", "Sen 1", "Sen 0"], await Titles());

        // The third line to the first: what was first is second, what was
        // second is third, and the fourth never moved.
        Assert.True(await _dreams.PlaceAsync(BoardA, made[1].Id, 1));

        Assert.Equal(["Sen 1", "Sen 3", "Sen 2", "Sen 0"], await Titles());
    }

    [Fact]
    public async Task A_dream_moved_down_pulls_the_ones_it_passed_up_by_one()
    {
        var made = await Made(4);

        Assert.True(await _dreams.PlaceAsync(BoardA, made[3].Id, 3));

        Assert.Equal(["Sen 2", "Sen 1", "Sen 3", "Sen 0"], await Titles());
    }

    [Fact]
    public async Task The_board_is_counted_off_from_nought_every_time()
    {
        var made = await Made(3);

        await _dreams.PlaceAsync(BoardA, made[0].Id, 2);

        Assert.Equal([0, 1, 2], (await _dreams.ListAsync(BoardA)).Select(d => d.SortOrder));
    }

    [Fact]
    public async Task A_place_past_the_end_is_the_end()
    {
        var made = await Made(3);

        await _dreams.PlaceAsync(BoardA, made[2].Id, 99);

        Assert.Equal(["Sen 1", "Sen 0", "Sen 2"], await Titles());
    }

    [Fact]
    public async Task Another_board_cannot_move_it_and_is_not_counted_among_it()
    {
        var made = await Made(2);
        var elsewhere = await _dreams.CreateAsync(BoardB, Input("Cizí"));

        Assert.False(await _dreams.PlaceAsync(BoardB, made[0].Id, 1));
        Assert.True(await _dreams.PlaceAsync(BoardA, made[0].Id, 1));

        Assert.Equal(["Sen 0", "Sen 1"], await Titles());
        Assert.Equal(elsewhere.SortOrder, (await _dreams.FindAsync(BoardB, elsewhere.Id))!.SortOrder);
    }
}
