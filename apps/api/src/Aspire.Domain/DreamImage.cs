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
