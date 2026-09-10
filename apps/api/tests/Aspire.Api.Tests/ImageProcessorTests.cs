using Aspire.Infrastructure.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class ImageProcessorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"aspire-test-{Guid.NewGuid():N}");

    public ImageProcessorTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string PathFor(string size) => Path.Combine(_dir, $"{size}.webp");

    /// <summary>A JPEG of the given size, optionally with an EXIF orientation and a GPS tag.</summary>
    private static MemoryStream Jpeg(int width, int height, ushort? orientation = null)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(220, 90, 40));
        if (orientation is { } o)
        {
            var exif = new ExifProfile();
            exif.SetValue(ExifTag.Orientation, o);
            exif.SetValue(ExifTag.GPSLatitude, new[] { new Rational(50), new Rational(5), new Rational(0) });
            image.Metadata.ExifProfile = exif;
        }

        var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder());
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task Three_sizes_come_out_and_none_is_larger_than_its_edge()
    {
        using var source = Jpeg(3000, 2000);

        var (width, height) = await ImageProcessor.ProcessAsync(source, PathFor);

        Assert.Equal((2048, 1365), (width, height));
        foreach (var (size, edge) in ImageProcessor.LongestEdge)
        {
            var info = await Image.IdentifyAsync(PathFor(size));
            Assert.True(Math.Max(info.Width, info.Height) <= edge, $"{size} is {info.Width}×{info.Height}");
            Assert.Equal(3.0 / 2.0, (double)info.Width / info.Height, 2);
        }
    }

    [Fact]
    public async Task A_small_picture_is_never_scaled_up()
    {
        using var source = Jpeg(300, 200);

        var (width, height) = await ImageProcessor.ProcessAsync(source, PathFor);

        Assert.Equal((300, 200), (width, height));
        var thumb = await Image.IdentifyAsync(PathFor("thumb"));
        Assert.Equal((300, 200), (thumb.Width, thumb.Height));
    }

    [Fact]
    public async Task The_phone_orientation_is_applied_and_the_exif_is_gone()
    {
        // Orientation 6: the sensor lay on its side, the picture is portrait.
        using var source = Jpeg(3000, 2000, orientation: 6);

        var (width, height) = await ImageProcessor.ProcessAsync(source, PathFor);

        Assert.True(height > width, $"{width}×{height} should be portrait");
        foreach (var size in MediaStore.Sizes)
        {
            using var saved = await Image.LoadAsync(PathFor(size));
            Assert.Null(saved.Metadata.ExifProfile);
        }
    }

    [Fact]
    public async Task Anything_that_is_not_a_picture_is_refused_at_the_gate()
    {
        using var text = new MemoryStream("not a photograph"u8.ToArray());
        using var picture = Jpeg(10, 10);

        Assert.False(await ImageProcessor.IsImageAsync(text));
        Assert.True(await ImageProcessor.IsImageAsync(picture));
        Assert.Equal(0, picture.Position);
    }
}
