using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Aspire.Infrastructure.Media;

/// <summary>
/// A handful of a board's photographs onto one phone-shaped canvas, for a
/// lock screen (PLAN.md §3.5). The arrangement is `CollageLayout`'s; this
/// only fills it.
///
/// JPEG, not WebP: the file leaves the app entirely — it is saved to a
/// photo library and then chosen as a wallpaper by the phone's own settings,
/// and JPEG is the format every one of those steps has always taken.
/// </summary>
public static class CollageRenderer
{
    /// <summary>
    /// The canvas a phone may ask for. The floor keeps a request from being
    /// a thumbnail; the ceiling keeps one from being a denial of service, at
    /// four times the tallest phone anybody is holding.
    /// </summary>
    public const int MinEdge = 200;
    public const int MaxEdge = 4096;

    private const int Quality = 88;

    /// <summary>The default canvas: an iPhone's, from PLAN.md §4.</summary>
    public const int DefaultWidth = 1170;
    public const int DefaultHeight = 2532;

    /// <summary>Whether a canvas is one this will draw on.</summary>
    public static bool IsCanvas(int width, int height) =>
        width is >= MinEdge and <= MaxEdge && height is >= MinEdge and <= MaxEdge;

    /// <summary>
    /// The collage, written as JPEG. Each photograph fills its cell and is
    /// cropped to it from the centre, so nothing is squashed and nothing is
    /// letterboxed — a wallpaper with a band of nothing in it is a wallpaper
    /// that looks like a mistake.
    /// </summary>
    public static async Task RenderAsync(
        IReadOnlyList<string> photographs,
        int width,
        int height,
        Stream destination,
        CancellationToken ct = default)
    {
        var cells = CollageLayout.For(photographs.Count, width, height);

        using var canvas = new Image<Rgb24>(width, height);
        for (var i = 0; i < photographs.Count; i++)
        {
            var cell = cells[i];
            using var photograph = await Image.LoadAsync(photographs[i], ct);
            photograph.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(cell.Width, cell.Height),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            canvas.Mutate(x => x.DrawImage(photograph, new Point(cell.X, cell.Y), 1f));
        }

        await canvas.SaveAsync(destination, new JpegEncoder { Quality = Quality }, ct);
    }
}
