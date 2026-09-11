namespace Aspire.Domain;

/// <summary>
/// The dream a board is showing today (PLAN.md §5), on the server's side.
///
/// The device works the same rule out for itself in `dreams/board.ts`, and
/// has to: it opens the board without a signal (D24, D25). This exists
/// because the morning nudge has to name a dream while the phone is asleep,
/// and it is the same rule so that the notification and the board agree
/// about what today's dream is — the nudge stamps <c>LastShownAt</c>, and
/// the board then opens on the dream the person was already told about.
/// </summary>
public static class DailyPick
{
    /// <summary>
    /// The board's dream for the day it is where the device is: the one
    /// already shown today when there is one, so it holds all day; otherwise
    /// the one shown least recently, and one never shown at all before any
    /// that has been.
    ///
    /// The offset is the device's, and both the clock and the stamps are read
    /// through it — a board opened at half past eleven at night in Prague was
    /// shown that day, not on the UTC morning after it.
    /// </summary>
    public static Dream? From(
        IReadOnlyList<Dream> dreams,
        DateTimeOffset utcNow,
        int utcOffsetMinutes,
        Random? random = null)
    {
        var candidates = dreams.Where(d => d.Status != DreamStatus.Achieved).ToList();
        if (candidates.Count == 0) return null;

        var today = LocalDate(utcNow, utcOffsetMinutes);
        var already = candidates.FirstOrDefault(d =>
            d.LastShownAt is { } shown && LocalDate(shown, utcOffsetMinutes) == today);
        if (already is not null) return already;

        var never = candidates.Where(d => d.LastShownAt is null).ToList();
        if (never.Count > 0) return never[(random ?? Random.Shared).Next(never.Count)];

        return candidates.MinBy(d => d.LastShownAt!.Value);
    }

    private static DateOnly LocalDate(DateTimeOffset instant, int utcOffsetMinutes) =>
        DateOnly.FromDateTime(NudgeSchedule.LocalNow(instant, utcOffsetMinutes));
}
