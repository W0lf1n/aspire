using Aspire.Api.Dreams;
using Aspire.Api.Images;
using Aspire.Domain;
using Aspire.Infrastructure;
using Aspire.Infrastructure.Media;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// Up to five dreamt photographs on a dream, in an order, under a template
/// (D82). The achieved photograph is its own thing and is left alone (D28).
/// </summary>
public sealed class FivePhotographsTests : IDisposable
{
    private const string Board = "board-a";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MediaStore _media;
    private readonly ImageService _images;
    private readonly DreamService _dreams;

    public FivePhotographsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        _db.Boards.Add(new Board { Id = Board, Name = "A" });
        _db.SaveChanges();

        _media = new MediaStore(Path.Combine(Path.GetTempPath(), $"aspire-media-{Guid.NewGuid():N}"));
        _images = new ImageService(_db, _media, new ImageQueue());
        _dreams = new DreamService(_db, _media);
    }

    public void Dispose()
    {
        foreach (var id in _db.DreamImages.Select(i => i.Id).ToList())
        {
            File.Delete(ImageService.StagedPath(id));
        }

        _db.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_media.Root)) Directory.Delete(_media.Root, recursive: true);
    }

    private Task<Dream> ADream() => _dreams.CreateAsync(Board, new DreamInput("Dům", null, null, null, null, null));

    private async Task<DreamImage> APhoto(Dream dream, DreamImageKind kind = DreamImageKind.Dreamt)
    {
        using var image = new Image<Rgba32>(40, 50, new Rgba32(90, 79, 214));
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;

        var (made, problem) = await _images.AddAsync(dream, stream, stream.Length, kind);
        Assert.Null(problem);
        return made!;
    }

    private async Task<List<Guid>> Order(Dream dream) =>
        (await _images.OfDreamsAsync([dream.Id]))
            .Where(i => i.Kind == DreamImageKind.Dreamt)
            .Select(i => i.Id)
            .ToList();

    [Fact]
    public async Task Five_fit_and_the_sixth_is_refused_with_a_sentence()
    {
        var dream = await ADream();
        for (var i = 0; i < Dream.PhotosMax; i++) await APhoto(dream);

        using var image = new Image<Rgba32>(40, 50);
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        var (sixth, problem) = await _images.AddAsync(dream, stream, stream.Length);

        Assert.Null(sixth);
        Assert.Equal("Sen má nejvýš 5 fotek. Některou nejdřív smaž.", problem);
        Assert.Equal(Dream.PhotosMax, (await Order(dream)).Count);
    }

    [Fact]
    public async Task The_achieved_photograph_is_not_one_of_the_five()
    {
        var dream = await ADream();
        for (var i = 0; i < Dream.PhotosMax; i++) await APhoto(dream);

        // The proof is its own photograph, not a sixth cell (D28).
        var proof = await APhoto(dream, DreamImageKind.Achieved);

        Assert.Equal(DreamImageKind.Achieved, proof.Kind);
    }

    [Fact]
    public async Task A_new_photograph_stands_behind_the_last_even_after_one_was_taken_out()
    {
        var dream = await ADream();
        var first = await APhoto(dream);
        var second = await APhoto(dream);
        var third = await APhoto(dream);

        await _images.RemoveAsync(dream, second.Id);
        var fourth = await APhoto(dream);

        // Counting the rows would have given it the third one's number, and
        // the tie would have fallen to the id.
        Assert.True(fourth.SortOrder > third.SortOrder);
        Assert.Equal([first.Id, third.Id, fourth.Id], await Order(dream));
    }

    [Fact]
    public async Task The_order_is_written_whole_and_the_first_is_the_cover()
    {
        var dream = await ADream();
        var a = await APhoto(dream);
        var b = await APhoto(dream);
        var c = await APhoto(dream);

        Assert.Null(await _images.ReorderAsync(dream, [c.Id, a.Id, b.Id]));

        Assert.Equal([c.Id, a.Id, b.Id], await Order(dream));
    }

    [Fact]
    public async Task An_order_has_to_name_every_dreamt_photograph_and_nothing_else()
    {
        var dream = await ADream();
        var other = await _dreams.CreateAsync(Board, new DreamInput("Loď", null, null, null, null, null));
        var a = await APhoto(dream);
        var b = await APhoto(dream);
        var proof = await APhoto(dream, DreamImageKind.Achieved);
        var elsewhere = await APhoto(other);

        const string refused = "Tohle nejsou fotky tohohle snu.";
        Assert.Equal(refused, await _images.ReorderAsync(dream, [a.Id]));
        Assert.Equal(refused, await _images.ReorderAsync(dream, [a.Id, a.Id]));
        Assert.Equal(refused, await _images.ReorderAsync(dream, [a.Id, elsewhere.Id]));
        Assert.Equal(refused, await _images.ReorderAsync(dream, [a.Id, b.Id, proof.Id]));

        Assert.Equal([a.Id, b.Id], await Order(dream));
    }

    [Fact]
    public async Task A_template_is_one_of_three_and_choosing_one_is_a_change_to_the_dream()
    {
        var dream = await ADream();
        var before = dream.UpdatedAt;
        await Task.Delay(5);

        var (chosen, problem) = await _dreams.LayoutAsync(Board, dream.Id, 2);

        Assert.Null(problem);
        Assert.Equal(2, chosen!.Layout);
        Assert.True(chosen.UpdatedAt > before);

        Assert.Equal("Takové rozložení není.", (await _dreams.LayoutAsync(Board, dream.Id, 3)).Problem);
        Assert.Equal("Takové rozložení není.", (await _dreams.LayoutAsync(Board, dream.Id, -1)).Problem);
        Assert.Equal((null, null), await _dreams.LayoutAsync("board-b", dream.Id, 1));
    }

    [Fact]
    public async Task A_dream_starts_on_the_first_template()
    {
        Assert.Equal(0, (await ADream()).Layout);
    }
}
