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

    // ── where a photograph is looked at (D54) ───────────────────────────────

    private async Task<(Dream Dream, DreamImage Image)> APhotograph()
    {
        var dream = await ADream();
        using var upload = Png(8, 8);
        var (image, _) = await _images.AddAsync(dream, upload, upload.Length);
        return (dream, image!);
    }

    [Fact]
    public async Task A_new_photograph_is_the_middle_and_all_of_it()
    {
        var (_, image) = await APhotograph();

        Assert.Equal(DreamImage.Centre, image.FocusX);
        Assert.Equal(DreamImage.Centre, image.FocusY);
        Assert.Equal(DreamImage.NoZoom, image.Zoom);
    }

    [Fact]
    public async Task An_upload_can_arrive_already_positioned()
    {
        // On the add screen there is no dream to hang a second request on, so
        // the crop travels with the photograph.
        var dream = await ADream();
        using var upload = Png(8, 8);

        var (image, problem) = await _images.AddAsync(
            dream, upload, upload.Length, DreamImageKind.Dreamt, new FocalInput(0.25, 0.75, 1.5));

        Assert.Null(problem);
        Assert.Equal(0.25, image!.FocusX);
        Assert.Equal(0.75, image.FocusY);
        Assert.Equal(1.5, image.Zoom);
    }

    [Fact]
    public async Task Moving_it_keeps_what_was_not_sent()
    {
        var (dream, image) = await APhotograph();
        await _images.MoveAsync(dream, image.Id, new FocalInput(0.2, 0.8, 2.0));

        var (moved, problem) = await _images.MoveAsync(dream, image.Id, new FocalInput(0.4, null, null));

        Assert.Null(problem);
        Assert.Equal(0.4, moved!.FocusX);
        Assert.Equal(0.8, moved.FocusY);
        Assert.Equal(2.0, moved.Zoom);
    }

    [Fact]
    public async Task Moving_it_touches_no_file()
    {
        // The crop is metadata, which is why it is instant and why it is right
        // for every shape at once.
        var (dream, image) = await APhotograph();
        await _images.ProcessAsync(image.Id);
        var screen = _media.PathOf(dream.Id, image.Id, "screen");
        var written = File.GetLastWriteTimeUtc(screen);

        await _images.MoveAsync(dream, image.Id, new FocalInput(0, 1, 3));

        Assert.True(File.Exists(screen));
        Assert.Equal(written, File.GetLastWriteTimeUtc(screen));
    }

    [Theory]
    [InlineData(-0.1, 0.5, 1.0, "Výřez je mimo fotku.")]
    [InlineData(1.1, 0.5, 1.0, "Výřez je mimo fotku.")]
    [InlineData(0.5, 2.0, 1.0, "Výřez je mimo fotku.")]
    [InlineData(0.5, 0.5, 0.5, "Přiblížení je mezi 1 a 3.")]
    [InlineData(0.5, 0.5, 4.0, "Přiblížení je mezi 1 a 3.")]
    public async Task A_crop_that_is_not_one_earns_a_sentence(
        double x, double y, double zoom, string expected)
    {
        var (dream, image) = await APhotograph();

        var (moved, problem) = await _images.MoveAsync(dream, image.Id, new FocalInput(x, y, zoom));

        Assert.Null(moved);
        Assert.Equal(expected, problem);
        var untouched = await _db.DreamImages.AsNoTracking().FirstAsync(i => i.Id == image.Id);
        Assert.Equal(DreamImage.Centre, untouched.FocusX);
    }

    [Fact]
    public async Task Another_dream_cannot_move_it()
    {
        var (_, image) = await APhotograph();
        var other = await ADream();

        var (moved, problem) = await _images.MoveAsync(other, image.Id, new FocalInput(0, 0, 1));

        Assert.Null(moved);
        Assert.Null(problem);
    }

    // ── what the board weighs (D64) ─────────────────────────────────────────

    [Fact]
    public async Task The_worker_weighs_the_files_and_the_board_is_their_sum()
    {
        var dream = await ADream();
        using var first = Png(1600, 1000);
        using var second = Png(800, 800);
        var (one, _) = await _images.AddAsync(dream, first, first.Length);
        var (two, _) = await _images.AddAsync(dream, second, second.Length);

        // Nothing weighs anything until the files exist.
        Assert.Equal((2, 0L), await _images.UsageOfAsync(Board));

        await _images.ProcessAsync(one!.Id);
        await _images.ProcessAsync(two!.Id);

        var rows = await _db.DreamImages.AsNoTracking().ToListAsync();
        foreach (var row in rows)
        {
            var onDisk = MediaStore.Sizes.Sum(size => new FileInfo(_media.PathOf(dream.Id, row.Id, size)).Length);
            Assert.Equal(onDisk, row.Bytes);
            Assert.True(row.Bytes > 0);
        }

        Assert.Equal((2, rows.Sum(r => r.Bytes)), await _images.UsageOfAsync(Board));
    }

    [Fact]
    public async Task Another_board_weighs_nothing_here()
    {
        _db.Boards.Add(new Board { Id = "board-b", Name = "B" });
        await _db.SaveChangesAsync();
        var theirs = await _dreams.CreateAsync("board-b", new DreamInput("Cizí", null, null, null, null, null));
        using var upload = Png(600, 400);
        var (image, _) = await _images.AddAsync(theirs, upload, upload.Length);
        await _images.ProcessAsync(image!.Id);

        Assert.Equal((0, 0L), await _images.UsageOfAsync(Board));
        var (count, bytes) = await _images.UsageOfAsync("board-b");
        Assert.Equal(1, count);
        Assert.True(bytes > 0);
    }

    [Fact]
    public async Task The_sweep_weighs_what_was_made_before_the_column_and_leaves_the_rest_alone()
    {
        var dream = await ADream();
        using var upload = Png(600, 400);
        var (image, _) = await _images.AddAsync(dream, upload, upload.Length);
        await _images.ProcessAsync(image!.Id);
        var weighed = (await _db.DreamImages.SingleAsync()).Bytes;

        // A row from before the column: processed, and zero.
        var old = await _db.DreamImages.SingleAsync();
        old.Bytes = 0;
        await _db.SaveChangesAsync();

        await _images.MeasureAsync();

        Assert.Equal(weighed, (await _db.DreamImages.AsNoTracking().SingleAsync()).Bytes);

        // A row still in line is not the sweep's to weigh: its files do not exist yet.
        using var waiting = Png(8, 8);
        var (pending, _) = await _images.AddAsync(dream, waiting, waiting.Length);
        await _images.MeasureAsync();
        Assert.Equal(0, (await _db.DreamImages.AsNoTracking().SingleAsync(i => i.Id == pending!.Id)).Bytes);
    }

    [Fact]
    public async Task A_full_board_refuses_the_next_photograph_with_a_sentence()
    {
        var dream = await ADream();
        using var first = Png(600, 400);
        using var second = Png(600, 400);

        var (image, none) = await _images.AddAsync(dream, first, first.Length);
        Assert.Null(none);
        await _images.ProcessAsync(image!.Id);
        var (_, held) = await _images.UsageOfAsync(Board);

        // A ceiling one byte short of the next upload: what is held counts,
        // and so does the whole of what is arriving.
        var tight = new ImageService(_db, _media, _queue, maxBoardBytes: held + second.Length - 1);
        var (refused, problem) = await tight.AddAsync(dream, second, second.Length);

        Assert.Null(refused);
        Assert.Equal(ImageService.BoardFull, problem);
        Assert.Single(await _db.DreamImages.ToListAsync());

        // One byte more and it fits.
        var roomy = new ImageService(_db, _media, _queue, maxBoardBytes: held + second.Length);
        second.Position = 0;
        Assert.Null((await roomy.AddAsync(dream, second, second.Length)).Problem);
    }

    [Fact]
    public async Task A_photograph_put_on_moved_or_taken_off_is_the_dream_being_changed()
    {
        var dream = await ADream();
        var stamps = new List<DateTimeOffset> { dream.UpdatedAt };
        using var png = Png(400, 500);

        var (image, _) = await _images.AddAsync(dream, png, png.Length);
        stamps.Add((await _dreams.FindAsync(Board, dream.Id))!.UpdatedAt);

        await Task.Delay(5);
        await _images.MoveAsync(dream, image!.Id, new FocalInput(0.2, null, null));
        stamps.Add((await _dreams.FindAsync(Board, dream.Id))!.UpdatedAt);

        await Task.Delay(5);
        await _images.RemoveAsync(dream, image.Id);
        stamps.Add((await _dreams.FindAsync(Board, dream.Id))!.UpdatedAt);

        // Later every time, which is all the screen needs of it (D77).
        Assert.Equal(stamps.OrderBy(s => s).ToList(), stamps);
        Assert.Equal(stamps.Count, stamps.Distinct().Count());
    }
}
