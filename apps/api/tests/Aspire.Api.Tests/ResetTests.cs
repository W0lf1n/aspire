using Aspire.Api.Boards;
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
/// Starting over (D80): what goes, what stays, and the sentence in front of it.
/// </summary>
public sealed class ResetTests : IDisposable
{
    private const string BoardA = "board-a";
    private const string BoardB = "board-b";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MediaStore _media;
    private readonly ImageService _images;
    private readonly DreamService _dreams;

    public ResetTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        _db.Boards.Add(new Board { Id = BoardA, Name = "A", LinkKey = "lock-screen-key" });
        _db.Boards.Add(new Board { Id = BoardB, Name = "B" });
        _db.Devices.Add(new Device
        {
            Id = "device-a",
            BoardId = BoardA,
            Name = "telefon",
            TokenHash = "hash",
            PairedAt = DateTimeOffset.UtcNow
        });
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

    private static DreamInput Input(string title) => new(title, null, null, null, null, null);

    private static MemoryStream Png()
    {
        using var image = new Image<Rgba32>(400, 500, new Rgba32(90, 79, 214));
        var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }

    [Theory]
    [InlineData("začínám znovu")]
    [InlineData("Začínám znovu")]
    [InlineData("ZAČÍNÁM ZNOVU")]
    [InlineData("zacinam znovu")]
    [InlineData("  začínám   znovu  ")]
    public void The_phrase_forgives_case_accents_and_stray_spaces(string typed) =>
        Assert.True(ResetPhrase.Matches(typed));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("začínám")]
    [InlineData("znovu začínám")]
    [InlineData("začínámznovu")]
    [InlineData("začínám znovu.")]
    [InlineData("ano")]
    public void Anything_else_is_not_the_phrase(string? typed) =>
        Assert.False(ResetPhrase.Matches(typed));

    [Fact]
    public async Task Every_dream_and_photograph_goes_and_the_files_with_them()
    {
        var dream = await _dreams.CreateAsync(BoardA, Input("Loď"));
        await _dreams.CreateAsync(BoardA, Input("Dům"));
        using var png = Png();
        var (image, _) = await _images.AddAsync(dream, png, png.Length);
        await _images.ProcessAsync(image!.Id);
        Assert.True(Directory.Exists(_media.DirectoryOf(dream.Id)));

        // One more, still waiting for the worker in the temp directory.
        using var waiting = Png();
        var (staged, _) = await _images.AddAsync(dream, waiting, waiting.Length);
        Assert.True(File.Exists(ImageService.StagedPath(staged!.Id)));

        Assert.Equal(2, await _dreams.ResetAsync(BoardA));

        Assert.Empty(await _dreams.ListAsync(BoardA));
        Assert.Empty(await _db.DreamImages.ToListAsync());
        Assert.False(Directory.Exists(_media.DirectoryOf(dream.Id)));
        Assert.False(File.Exists(ImageService.StagedPath(staged.Id)));
    }

    [Fact]
    public async Task What_is_about_the_board_rather_than_a_dream_stays()
    {
        await _dreams.CreateAsync(BoardA, Input("Loď"));

        await _dreams.ResetAsync(BoardA);

        var board = await _db.Boards.AsNoTracking().FirstAsync(b => b.Id == BoardA);
        Assert.Equal("lock-screen-key", board.LinkKey);
        Assert.Single(await _db.Devices.Where(d => d.BoardId == BoardA).ToListAsync());
    }

    [Fact]
    public async Task Another_board_is_not_touched()
    {
        await _dreams.CreateAsync(BoardA, Input("Moje"));
        await _dreams.CreateAsync(BoardB, Input("Cizí"));

        await _dreams.ResetAsync(BoardA);

        Assert.Equal(["Cizí"], (await _dreams.ListAsync(BoardB)).Select(d => d.Title));
    }

    [Fact]
    public async Task An_empty_board_starts_over_without_complaint()
    {
        Assert.Equal(0, await _dreams.ResetAsync(BoardA));
    }

    [Fact]
    public async Task The_board_after_is_a_board_a_dream_can_be_written_on()
    {
        await _dreams.CreateAsync(BoardA, Input("Před"));
        await _dreams.ResetAsync(BoardA);

        var after = await _dreams.CreateAsync(BoardA, Input("Po"));

        Assert.Equal(["Po"], (await _dreams.ListAsync(BoardA)).Select(d => d.Title));
        Assert.Equal(0, after.SortOrder);
    }
}
