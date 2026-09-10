namespace Aspire.Domain;

/// <summary>
/// One photograph of a dream: three sizes on disk and one row here. The row
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

    /// <summary>The full size's dimensions, after orientation; zero until processed.</summary>
    public int Width { get; set; }
    public int Height { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
