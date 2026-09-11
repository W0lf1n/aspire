namespace Aspire.Domain;

/// <summary>
/// The three areas a dream can belong to (PLAN.md §3.1): something to want,
/// something to be, something to do.
///
/// A fixed set rather than a table of the board's own (§8.3, D43): the same
/// argument that keeps <see cref="DreamStatus"/> three words. A category a
/// board could rename would need a screen to rename it in, and this app has
/// so far refused to grow one. It was Yager's nine until D43 (D32); three
/// fit a row, and the question they ask — want, be, or do — is one a dream
/// can answer in the second it takes to write it down.
///
/// A dream may have none. Adding one is three taps and a photograph, and a
/// question it must answer first is a question that stops it being written.
/// </summary>
public enum DreamCategory
{
    Want,
    Be,
    Do
}

/// <summary>
/// The category as it travels and as it is stored: one lower-case word in
/// both places and in `packages/contracts`, the way the status and the image
/// kind already are.
/// </summary>
public static class DreamCategoryNames
{
    public static string ToWire(DreamCategory category) => category switch
    {
        DreamCategory.Want => "want",
        DreamCategory.Be => "be",
        DreamCategory.Do => "do",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

    /// <summary>
    /// A stored word back into the set. The nine of D32 were cleared by the
    /// <c>Areas</c> migration, so anything else in the column is a row this
    /// build has never written and the 500 it earns is the truth.
    /// </summary>
    public static DreamCategory Parse(string value) => value switch
    {
        "want" => DreamCategory.Want,
        "be" => DreamCategory.Be,
        "do" => DreamCategory.Do,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
