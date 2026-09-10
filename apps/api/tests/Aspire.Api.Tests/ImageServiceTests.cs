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

public sealed class ImageServiceTests : IDisposable
{
    private const string Board = "board-a";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly MediaStore _media;
    private readonly ImageQueue _queue = new();
    private readonly ImageService _images;
    private readonly DreamService _dreams;

    public ImageServiceTests()
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
        _images = new ImageService(_db, _media, _queue);
        _dreams = new DreamService(_db, _media);
    }

    public void Dispose()
    {
        // An upload left in line is a file in the temp directory; not ours to leave.
        foreach (var id in _db.DreamImages.Select(i => i.Id).ToList())
        {
            File.Delete(ImageService.StagedPath(id));
        }

        _db.Dispose();
        _connection.Dispose();
        if (Directory.Exists(_media.Root)) Directory.Delete(_media.Root, recursive: true);
    }

    private static MemoryStream Png(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(90, 79, 214));
        var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }

    private Task<Dream> ADream() => _dreams.CreateAsync(Board, new DreamInput("Loď", null, null, null, null, null));

    [Fact]
    public async Task An_upload_is_a_dreamt_photograph_unless_it_says_otherwise()
    {
        var dream = await ADream();
        using var dreamt = Png(400, 500);
        using var achieved = Png(400, 500);

        var (first, _) = await _images.AddAsync(dream, dreamt, dreamt.Length);
        var (second, _) = await _images.AddAsync(dream, achieved, achieved.Length, DreamImageKind.Achieved);

        Assert.Equal(DreamImageKind.Dreamt, first!.Kind);
        Assert.Equal(DreamImageKind.Achieved, second!.Kind);

        // Both kinds live under the one dream, and the kind survives the round
        // trip through the column's string conversion.
        var stored = await _images.OfDreamsAsync([dream.Id]);
        Assert.Equal(
            [DreamImageKind.Dreamt, DreamImageKind.Achieved],
            stored.Select(i => i.Kind));
    }

    [Theory]
    [InlineData(null, DreamImageKind.Dreamt)]
    [InlineData("", DreamImageKind.Dreamt)]
    [InlineData("dreamt", DreamImageKind.Dreamt)]
    [InlineData("achieved", DreamImageKind.Achieved)]
    public void A_request_naming_a_kind_is_read(string? asked, DreamImageKind expected)
    {
        Assert.Equal(expected, DreamImageKindNames.TryParse(asked));
    }

    [Theory]
    [InlineData("Achieved")]
    [InlineData("real")]
    [InlineData("splneno")]
    public void A_request_naming_anything_else_is_no_kind_at_all(string asked)
    {
        Assert.Null(DreamImageKindNames.TryParse(asked));
    }

    [Fact]
    public async Task An_upload_makes_a_row_and_the_worker_makes_the_files()
    {
        var dream = await ADream();
        using var upload = Png(1600, 1000);

        var (image, problem) = await _images.AddAsync(dream, upload, upload.Length);

        Assert.Null(problem);
        Assert.Null(image!.ProcessedAt);
        Assert.True(File.Exists(ImageService.StagedPath(image.Id)));

        await _images.ProcessAsync(image.Id);

        var stored = await _db.DreamImages.SingleAsync();
        Assert.NotNull(stored.ProcessedAt);
        Assert.Equal((1600, 1000), (stored.Width, stored.Height));
        Assert.False(File.Exists(ImageService.StagedPath(image.Id)));
        foreach (var size in MediaStore.Sizes)
        {
            Assert.True(File.Exists(_media.PathOf(dream.Id, image.Id, size)), size);
        }
    }

    [Fact]
    public async Task The_gate_refuses_nothing_too_much_and_not_a_picture()
    {
        var dream = await ADream();

        using var empty = new MemoryStream();
        Assert.Equal("Vyber fotku.", (await _images.AddAsync(dream, empty, 0)).Problem);

        using var small = Png(4, 4);
        Assert.Equal("Fotka je moc velká, nejvýš 10 MB.",
            (await _images.AddAsync(dream, small, ImageService.MaxUploadBytes + 1)).Problem);

        using var text = new MemoryStream("not a photograph"u8.ToArray());
        Assert.Equal("Tohle není obrázek.", (await _images.AddAsync(dream, text, text.Length)).Problem);

        Assert.Empty(await _db.DreamImages.ToListAsync());
    }

    [Fact]
    public async Task A_row_whose_upload_is_gone_is_dropped_by_the_sweep()
    {
        var dream = await ADream();
        using var upload = Png(8, 8);
        var (image, _) = await _images.AddAsync(dream, upload, upload.Length);
        File.Delete(ImageService.StagedPath(image!.Id));

        Assert.Equal([image.Id], await _images.UnprocessedAsync());
        await _images.ProcessAsync(image.Id);

        Assert.Empty(await _db.DreamImages.ToListAsync());
    }

    [Fact]
    public async Task Removing_an_image_removes_its_files_and_removing_the_dream_removes_them_all()
    {
        var dream = await ADream();
        using var first = Png(8, 8);
        using var second = Png(8, 8);
        var (one, _) = await _images.AddAsync(dream, first, first.Length);
        var (two, _) = await _images.AddAsync(dream, second, second.Length);
        await _images.ProcessAsync(one!.Id);
        await _images.ProcessAsync(two!.Id);
        Assert.Equal(1, two.SortOrder);

        Assert.True(await _images.RemoveAsync(dream, one.Id));
        Assert.False(Directory.Exists(_media.DirectoryOf(dream.Id, one.Id)));
        Assert.True(Directory.Exists(_media.DirectoryOf(dream.Id, two.Id)));
        Assert.False(await _images.RemoveAsync(dream, one.Id));

        Assert.True(await _dreams.DeleteAsync(Board, dream.Id));
        Assert.False(Directory.Exists(_media.DirectoryOf(dream.Id)));
        Assert.Empty(await _db.DreamImages.ToListAsync());
    }

    [Fact]
    public async Task Another_dream_cannot_remove_it()
    {
        var mine = await ADream();
        var other = await ADream();
        using var upload = Png(8, 8);
        var (image, _) = await _images.AddAsync(mine, upload, upload.Length);

        Assert.False(await _images.RemoveAsync(other, image!.Id));
        Assert.Single(await _db.DreamImages.ToListAsync());
    }
}
