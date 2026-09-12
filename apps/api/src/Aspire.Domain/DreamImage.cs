namespace Aspire.Domain;

/// <summary>
/// One photograph of a dream, of one of two kinds: three sizes on disk and
/// one row here. The row
/// exists from the upload; the sizes follow a moment later, and
/// <see cref="ProcessedAt"/> says when they did. The files live at
/// <c>{media}/{dreamId}/{imageId}/{size}.webp</c>: a new photograph is a
/// new id, so a URL cached for a year never shows the wrong picture (D23).
/// </summary>
public sealed class DreamImage
{
    public Guid Id { get; set; }
    public Guid DreamId { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// The dreamt photograph or the achieved one. The board and the reel
    /// show the dreamt; the Síň slávy stands the two side by side, which is
    /// the whole of PLAN.md §3.3's proof that the thing came true (D28).
    /// </summary>
    public DreamImageKind Kind { get; set; } = DreamImageKind.Dreamt;

    /// <summary>The full size's dimensions, after orientation; zero until processed.</summary>
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>
    /// What the three files weigh together, once they exist; zero until then.
    /// Kept on the row so a board's size is one sum rather than a walk of the
    /// disk, which is what the ceiling and the settings screen read (D64).
    /// </summary>
    public long Bytes { get; set; }

    /// <summary>
    /// Where this photograph is looked at, across and down, and how far in
    /// (D54). 0.5 · 0.5 · 1 is the middle and all of it, which is what every
    /// photograph taken before this was stored as.
    ///
    /// They are <c>object-position</c> percentages, 0 to 1, so the client sets
    /// two custom properties and does no arithmetic, and
    /// <see cref="Media.FocalCrop"/> works the same window out for a collage
    /// cell. Metadata rather than a cropped file: every surface crops to a
    /// different shape — a screen, a 4:5 print, a 40 px circle, a collage cell
    /// — and pixels cropped for one of them are wrong for the other three.
    /// </summary>
    public double FocusX { get; set; } = Centre;
    public double FocusY { get; set; } = Centre;
    public double Zoom { get; set; } = NoZoom;

    public const double Centre = 0.5;
    public const double NoZoom = 1.0;

    /// <summary>
    /// As close as it goes. Past three the screen size is being stretched on a
    /// phone, and the point of this is a better crop, not a worse picture.
    /// </summary>
    public const double MaxZoom = 3.0;

    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>What a photograph is of: the dream, or the dream come true.</summary>
public enum DreamImageKind
{
    Dreamt,
    Achieved
}

/// <summary>
/// The kind as it travels and as it is stored, kebab-case in both places and
/// in `packages/contracts`, the way <see cref="DreamStatusNames"/> does it.
/// </summary>
public static class DreamImageKindNames
{
    public static string ToWire(DreamImageKind kind) => kind switch
    {
        DreamImageKind.Dreamt => "dreamt",
        DreamImageKind.Achieved => "achieved",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    public static DreamImageKind Parse(string value) => value switch
    {
        "dreamt" => DreamImageKind.Dreamt,
        "achieved" => DreamImageKind.Achieved,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };

    /// <summary>The kind a request asked for, or null when it named nothing valid.</summary>
    public static DreamImageKind? TryParse(string? value) => value switch
    {
        null or "" => DreamImageKind.Dreamt,
        "dreamt" => DreamImageKind.Dreamt,
        "achieved" => DreamImageKind.Achieved,
        _ => null
    };
}
