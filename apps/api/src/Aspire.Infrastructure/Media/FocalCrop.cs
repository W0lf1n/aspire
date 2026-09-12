using SixLabors.ImageSharp;

namespace Aspire.Infrastructure.Media;

/// <summary>
/// Which part of a photograph a cell of a given shape shows (D54).
///
/// The browser does this with two CSS properties; this is the same arithmetic
/// in C#, so the wallpaper crops a photograph exactly where the reel does.
/// Pure geometry with no image in it, like <see cref="CollageLayout"/> beside
/// it, so every edge of it is a test rather than a picture somebody has to
/// look at.
///
/// The two numbers are <c>object-position</c> percentages: on the axis that
/// overflows, 0 shows the left or top edge, 1 the right or bottom, 0.5 the
/// middle. At zoom 1 exactly one axis overflows — which is what makes
/// „cover“ cover — and moving along the other one does nothing, because there
/// is nothing more of it to see.
/// </summary>
public static class FocalCrop
{
    /// <summary>
    /// The rectangle of the source to take, before it is resized to the cell.
    ///
    /// With the middle and no zoom this is what
    /// <c>ResizeMode.Crop</c> with a centre anchor already did, which is why
    /// every photograph taken before this looks the same afterwards.
    /// </summary>
    public static Rectangle For(
        int sourceWidth,
        int sourceHeight,
        int cellWidth,
        int cellHeight,
        double focusX,
        double focusY,
        double zoom)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0 || cellWidth <= 0 || cellHeight <= 0)
        {
            return new Rectangle(0, 0, Math.Max(1, sourceWidth), Math.Max(1, sourceHeight));
        }

        // Cover, then in by the zoom. A window smaller than the source is the
        // only thing that leaves anything to choose.
        var cover = Math.Max((double)cellWidth / sourceWidth, (double)cellHeight / sourceHeight);
        var scale = cover * Clamp(zoom, 1.0, double.MaxValue);

        var window = Math.Min(sourceWidth, cellWidth / scale);
        var height = Math.Min(sourceHeight, cellHeight / scale);

        // At least one pixel: a cell far narrower than the source at full zoom
        // can round its way to nothing.
        var w = Math.Max(1, (int)Math.Round(window));
        var h = Math.Max(1, (int)Math.Round(height));
        w = Math.Min(w, sourceWidth);
        h = Math.Min(h, sourceHeight);

        // Where the window sits in what is left over, which is the whole
        // meaning of the two numbers. No room left over is no choice to make.
        var x = (int)Math.Round((sourceWidth - w) * Clamp(focusX, 0, 1));
        var y = (int)Math.Round((sourceHeight - h) * Clamp(focusY, 0, 1));

        return new Rectangle(x, y, w, h);
    }

    private static double Clamp(double value, double low, double high) =>
        double.IsNaN(value) ? low : Math.Min(high, Math.Max(low, value));
}
