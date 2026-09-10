namespace Aspire.Domain;

/// <summary>
/// One dream on the board: a title, the one-line why, the line you say to
/// yourself, and where it stands.
///
/// The voice memo arrives with its own milestone (PLAN.md §3.4). What is
/// here is the shape M0 committed to, plus what M3 added to it.
/// </summary>
public sealed class Dream
{
    public const int TitleMaxLength = 120;
    public const int WhyMaxLength = 500;
    public const int AffirmationMaxLength = 120;

    public Guid Id { get; set; }

    /// <summary>The board this dream belongs to. Nothing crosses between boards.</summary>
    public required string BoardId { get; set; }
    public required string Title { get; set; }
    public string Why { get; set; } = string.Empty;

    /// <summary>
    /// The dream said as though it were already true, in the person's own
    /// words — „Bydlím u lesa“. Optional, and empty when there is none. The why
    /// explains the dream to you; this one states it, which is why the reel
    /// shows it in the why's place when a dream has one (D27).
    /// </summary>
    public string Affirmation { get; set; } = string.Empty;

    public DreamStatus Status { get; set; } = DreamStatus.Dreaming;

    /// <summary>Board order — the person's own priority, set by dragging.</summary>
    public int SortOrder { get; set; }

    public int? TargetYear { get; set; }

    /// <summary>
    /// Taps on the heart, counted, never per device: the board's own fuel
    /// gauge, not a social signal. Likes stay within the board (D22).
    /// </summary>
    public int Likes { get; set; }
    public DateTimeOffset? AchievedAt { get; set; }

    /// <summary>The daily pick reads this: least recently seen first (PLAN.md §5).</summary>
    public DateTimeOffset? LastShownAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum DreamStatus
{
    Dreaming,
    InProgress,
    Achieved
}

/// <summary>
/// The status as it travels and as it is stored: the same three words in
/// kebab-case on the wire, in the database and in `packages/contracts`.
/// </summary>
public static class DreamStatusNames
{
    public static string ToWire(DreamStatus status) => status switch
    {
        DreamStatus.Dreaming => "dreaming",
        DreamStatus.InProgress => "in-progress",
        DreamStatus.Achieved => "achieved",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static DreamStatus Parse(string value) => value switch
    {
        "dreaming" => DreamStatus.Dreaming,
        "in-progress" => DreamStatus.InProgress,
        "achieved" => DreamStatus.Achieved,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
