namespace Aspire.Infrastructure.Media;

/// <summary>Where one photograph goes on the canvas, in pixels.</summary>
public readonly record struct CollageCell(int X, int Y, int Width, int Height);

/// <summary>
/// How a handful of photographs are arranged on a phone-shaped canvas
/// (PLAN.md §3.5). Pure geometry: no image, no disk, no ImageSharp, so the
/// arrangement can be checked without rendering anything.
///
/// The canvas is filled edge to edge and every cell is used. A gutter would
/// need a colour, and the one place a colour exists in this project is
/// `tokens.css`, which a C# renderer cannot read (rule 1); a seamless
/// collage is also the better wallpaper.
/// </summary>
public static class CollageLayout
{
    /// <summary>
    /// More than six photographs on a phone screen is a mosaic rather than a
    /// wallpaper: each one is too small to be the dream you recognise.
    /// </summary>
    public const int MaxPhotographs = 6;

    /// <summary>
    /// The rows the photographs fall into. Up to three they stack, because a
    /// phone canvas is roughly twice as tall as it is wide and a full-width
    /// band is the shape a photograph survives; above three they pair up.
    /// </summary>
    public static int RowsFor(int count) => count <= 3 ? count : (count + 1) / 2;

    /// <summary>
    /// The cells, in order, filling the canvas. Rows share the height; the
    /// photographs are spread across them as evenly as they go, the earlier
    /// rows taking the extra, and each row's cells share its width. Rounding
    /// is carried rather than repeated, so the last cell of a row ends exactly
    /// on the edge and the last row ends exactly on the bottom — a canvas with
    /// a one-pixel seam of nothing down it is a wallpaper somebody notices.
    /// </summary>
    public static IReadOnlyList<CollageCell> For(int count, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxPhotographs);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        var rows = RowsFor(count);
        var cells = new List<CollageCell>(count);

        var y = 0;
        for (var row = 0; row < rows; row++)
        {
            // The remaining photographs over the remaining rows, rounded up:
            // [2, 2, 1] for five in three rows, never [1, 2, 2].
            var placed = cells.Count;
            var inRow = (count - placed + (rows - row - 1)) / (rows - row);
            var bottom = (int)((long)height * (row + 1) / rows);

            var x = 0;
            for (var column = 0; column < inRow; column++)
            {
                var right = (int)((long)width * (column + 1) / inRow);
                cells.Add(new CollageCell(x, y, right - x, bottom - y));
                x = right;
            }

            y = bottom;
        }

        return cells;
    }
}
