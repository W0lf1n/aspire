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
    /// One photograph on a collage: where its file is, and where it is looked
    /// at (D54). The crop travels with the picture, so the wallpaper shows the
    /// part of it the reel shows rather than whatever is in the middle.
    /// </summary>
    public readonly record struct Photograph(string Path, double FocusX, double FocusY, double Zoom)
    {
        /// <summary>The middle and all of it, for a caller with nothing to say.</summary>
        public static Photograph At(string path) => new(path, 0.5, 0.5, 1.0);
    }

    /// <summary>
    /// The collage, written as JPEG. Each photograph fills its cell and is
    /// cropped to it at its own focal point, so nothing is squashed and
    /// nothing is letterboxed — a wallpaper with a band of nothing in it is a
    /// wallpaper that looks like a mistake — and a face near the top of a
    /// portrait is still in the cell (D54).
    /// </summary>
    public static async Task RenderAsync(
        IReadOnlyList<Photograph> photographs,
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
            var chosen = photographs[i];
            using var photograph = await Image.LoadAsync(chosen.Path, ct);

            var window = FocalCrop.For(
                photograph.Width,
                photograph.Height,
                cell.Width,
                cell.Height,
                chosen.FocusX,
                chosen.FocusY,
                chosen.Zoom);

            // Crop to what is looked at, then scale that to the cell. The two
            // steps together are what `ResizeMode.Crop` did in one, with the
            // window put where the person put it rather than in the middle.
            photograph.Mutate(x => x.Crop(window).Resize(cell.Width, cell.Height));

            canvas.Mutate(x => x.DrawImage(photograph, new Point(cell.X, cell.Y), 1f));
        }

        await canvas.SaveAsync(destination, new JpegEncoder { Quality = Quality }, ct);
    }
}
