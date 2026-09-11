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
    public async Task A_new_dream_joins_the_end_of_its_own_board()
    {
        await _dreams.CreateAsync(BoardA, Input("První"));
        await _dreams.CreateAsync(BoardB, Input("Cizí"));
        var second = await _dreams.CreateAsync(BoardA, Input("Druhý"));

        Assert.Equal(1, second.SortOrder);
        var listed = await _dreams.ListAsync(BoardA);
        Assert.Equal(["První", "Druhý"], listed.Select(d => d.Title));
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
}
