namespace Aspire.Domain;

/// <summary>
/// One dream on the board: a title, the one-line why, and where it stands.
///
/// The images, the affirmation and the voice memo arrive with their own
/// milestones (PLAN.md §3). What is here is the shape M0 commits to, so the
/// first migration is not immediately followed by a second one.
/// </summary>
public sealed class Dream
{
    public const int TitleMaxLength = 120;
    public const int WhyMaxLength = 500;

    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Why { get; set; } = string.Empty;
    public DreamStatus Status { get; set; } = DreamStatus.Dreaming;

    /// <summary>Board order — the person's own priority, set by dragging.</summary>
    public int SortOrder { get; set; }

    public int? TargetYear { get; set; }
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
