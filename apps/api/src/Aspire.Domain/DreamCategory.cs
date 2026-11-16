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
