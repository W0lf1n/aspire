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

    /// <summary>
    /// How many dreams can be on Teď at once (D53). Ten is small enough to be
    /// swiped through in ten swipes, which is what makes the second reel worth
    /// having: a list you can reach the end of.
    /// </summary>
    public const int FocusMax = 10;

    /// <summary>
    /// How many dreamt photographs a dream can have (D82). One is a picture of
    /// the thing; five is the thing from every side — the house, the garden,
    /// the view, the kitchen, the door. Past that a tile stops being a collage
    /// and starts being a contact sheet, and no cell on a phone is big enough
    /// to be looked at.
    /// </summary>
    public const int PhotosMax = 5;

    /// <summary>How many collage templates there are for any count of photographs.</summary>
    public const int LayoutsMax = 3;

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

    /// <summary>
    /// Which of the three areas it belongs to, or none: want, be, do. A
    /// fixed set, not a table of the board's own (D43); optional, because a
    /// question a dream must answer before it can be written is a dream that
    /// does not get written.
    /// </summary>
    public DreamCategory? Category { get; set; }

    /// <summary>
    /// Board order, lowest first: the order the Seznam is in, which is the
    /// person's own — dragged, or typed into a line's number (D79). A new
    /// dream goes in front of the lowest. The reel does not read it except to
    /// break a tie: Vše is still shuffled (D30).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Where this dream stands on Teď, the second reel, or null when it is
    /// not on it (D53).
    ///
    /// An ordering key rather than a position: the screen numbers the ten by
    /// their place in the list, so a gap left when one is taken off costs
    /// nothing and there is no compaction to get wrong. Reordering rewrites
    /// the lot as 1…N, which is also what puts any drift back.
    ///
    /// Unlike <see cref="SortOrder"/> this really is the person's priority,
    /// and unlike the main reel it is not shuffled: ten dreams are reached in
    /// ten swipes, so a fixed order is the point rather than a route learned
    /// by heart (D30).
    /// </summary>
    public int? FocusRank { get; set; }

    public int? TargetYear { get; set; }

    /// <summary>
    /// Which of the collage templates the tile uses, for however many
    /// photographs the dream has: 0, 1 or 2 (D82). A variant rather than a
    /// template's name, because what it picks among depends on the count —
    /// the second template for three photographs is not the second for five —
    /// and a number survives a photograph being added or taken away where a
    /// name would have to be translated. The templates themselves are the
    /// client's (<c>dreams/collage.ts</c>): the server never draws one.
    /// </summary>
    public int Layout { get; set; }

    /// <summary>
    /// The key in this dream's share link, or null when it has never been
    /// shared (D61). The same thirty-two bytes the board's lock-screen link
    /// uses, against one dream instead of a whole board: what it opens is a
    /// page with this photograph and this title on it and nothing else — no
    /// other dream, no way back into the board, and nothing to write.
    ///
    /// Making a new one is how the old link is revoked, which is the only kind
    /// of revoking a random number has.
    /// </summary>
    public string? LinkKey { get; set; }

    /// <summary>Mirrors <c>ShareKey.MaxLength</c>: 43 characters, with room.</summary>
    public const int LinkKeyMaxLength = 64;

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
