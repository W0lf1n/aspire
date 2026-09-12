using Aspire.Infrastructure.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class CollageRendererTests : IDisposable
{
    private readonly string _dir =
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"aspire-collage-{Guid.NewGuid():N}")).FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    /// <summary>A WebP of one flat colour, as the media store holds them.</summary>
    private CollageRenderer.Photograph Photograph(Rgba32 colour, int width = 800, int height = 1000)
    {
        var path = Path.Combine(_dir, $"{colour.ToHex()}-{width}x{height}.webp");
        using var image = new Image<Rgba32>(width, height, colour);
        image.Save(path, new WebpEncoder());
        return CollageRenderer.Photograph.At(path);
    }

    /// <summary>A wide WebP, red on its left half and blue on its right.</summary>
    private string TwoSided(int width = 2000, int height = 1000)
    {
        var path = Path.Combine(_dir, $"sides-{Guid.NewGuid():N}.webp");
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(rows =>
        {
            for (var y = 0; y < rows.Height; y++)
            {
                var row = rows.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    row[x] = x < width / 2 ? new Rgba32(220, 30, 30) : new Rgba32(30, 30, 220);
                }
            }
        });
        image.Save(path, new WebpEncoder());
        return path;
    }

    private static Rgb24 At(Image<Rgb24> image, int x, int y) => image[x, y];

    [Fact]
    public async Task One_photograph_fills_the_whole_canvas()
    {
        var red = new Rgba32(220, 40, 40);
        using var jpeg = new MemoryStream();

        await CollageRenderer.RenderAsync([Photograph(red)], 400, 900, jpeg);

        jpeg.Position = 0;
        using var made = Image.Load<Rgb24>(jpeg);
        Assert.Equal(400, made.Width);
        Assert.Equal(900, made.Height);
        // Every corner is the photograph: nothing letterboxed, nothing empty.
        foreach (var (x, y) in new[] { (2, 2), (397, 2), (2, 897), (397, 897), (200, 450) })
        {
            Assert.InRange(At(made, x, y).R, 190, 255);
            Assert.InRange(At(made, x, y).G, 0, 90);
        }
    }

    [Fact]
    public async Task Two_photographs_are_a_top_and_a_bottom_in_the_order_given()
    {
        var green = new Rgba32(30, 160, 60);
        var blue = new Rgba32(30, 60, 200);
        using var jpeg = new MemoryStream();

        await CollageRenderer.RenderAsync([Photograph(green), Photograph(blue)], 400, 900, jpeg);

        jpeg.Position = 0;
        using var made = Image.Load<Rgb24>(jpeg);
        var top = At(made, 200, 200);
        var bottom = At(made, 200, 700);

        Assert.True(top.G > top.B, "the first photograph is the top band");
        Assert.True(bottom.B > bottom.G, "the second photograph is the bottom band");
    }

    [Fact]
    public async Task A_wide_photograph_is_cropped_rather_than_squashed()
    {
        // 2000 x 500 into a tall cell: covered and centre-cropped, so the
        // canvas is full colour rather than a band with nothing above it.
        var amber = new Rgba32(230, 150, 40);
        using var jpeg = new MemoryStream();

        await CollageRenderer.RenderAsync([Photograph(amber, 2000, 500)], 400, 900, jpeg);

        jpeg.Position = 0;
        using var made = Image.Load<Rgb24>(jpeg);
        foreach (var y in new[] { 3, 450, 896 })
        {
            Assert.InRange(At(made, 200, y).R, 200, 255);
            Assert.InRange(At(made, 200, y).B, 0, 110);
        }
    }

    [Fact]
    public async Task A_photograph_is_cropped_where_it_is_looked_at()
    {
        // Red on the left half, blue on the right, into a tall cell that can
        // only hold a slice of it. Which slice is the whole of D54: the same
        // two numbers the reel positions with, honoured by the wallpaper.
        var sides = TwoSided();

        Assert.True(await IsRed(new CollageRenderer.Photograph(sides, 0, 0.5, 1)), "nought looks left");
        Assert.False(await IsRed(new CollageRenderer.Photograph(sides, 1, 0.5, 1)), "one looks right");
    }

    /// <summary>Whether the middle of a one-photograph collage came out red.</summary>
    private static async Task<bool> IsRed(CollageRenderer.Photograph photograph)
    {
        using var jpeg = new MemoryStream();
        await CollageRenderer.RenderAsync([photograph], 400, 900, jpeg);

        jpeg.Position = 0;
        using var made = Image.Load<Rgb24>(jpeg);
        var middle = At(made, 200, 450);
        return middle.R > middle.B;
    }

    [Fact]
    public async Task It_is_a_jpeg()
    {
        using var jpeg = new MemoryStream();
        await CollageRenderer.RenderAsync([Photograph(new Rgba32(10, 10, 10))], 400, 900, jpeg);

        jpeg.Position = 0;
        Assert.IsType<JpegFormat>(Image.DetectFormat(jpeg));
    }

    [Theory]
    [InlineData(CollageRenderer.MinEdge, CollageRenderer.MaxEdge, true)]
    [InlineData(1170, 2532, true)]
    [InlineData(CollageRenderer.MinEdge - 1, 2532, false)]
    [InlineData(1170, CollageRenderer.MaxEdge + 1, false)]
    [InlineData(0, 0, false)]
    public void A_canvas_it_will_not_draw_on_is_named_before_anything_is_loaded(
        int width, int height, bool ok)
    {
        Assert.Equal(ok, CollageRenderer.IsCanvas(width, height));
    }
}
