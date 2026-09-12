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
///
/// Since D58 the rule reads the heart: the dream with the most fuel, which is
/// how long it has waited multiplied by what its hearts add. Both halves count
/// days the same way and cap the hearts the same way, because a notification
/// naming one dream while the board opens on another is worse than either
/// rule on its own.
/// </summary>
public static class DailyPick
{
    /// <summary>
    /// The most hearts that count, mirroring `LIKES_CAP` in `board.ts`. Above
    /// ten the number goes up and the pick stops listening, and the cap is the
    /// point: with no ceiling, ten loved dreams would take every morning
    /// between them and the rest of the board would never come back (D58).
    /// </summary>
    public const int LikesCap = 10;

    /// <summary>
    /// The board's dream for the day it is where the device is: the one
    /// already shown today when there is one, so it holds all day; otherwise
    /// one never shown at all; otherwise the one with the most fuel.
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

        // `MaxBy` keeps the first of equals, and the board arrives ordered by
        // `SortOrder`, so a tie falls to board order exactly as it does on the
        // client — two devices answer the same without storing the answer.
        return candidates.MaxBy(d => FuelOf(d, utcNow, utcOffsetMinutes));
    }

    /// <summary>
    /// How overdue a dream is, as the heart weighs it — the twin of `fuel` in
    /// `board.ts` (D58):
    ///
    ///     days since shown × (1 + min(likes, 10) / 10)
    ///
    /// Ten hearts double it, so a loved dream comes round twice as often as one
    /// with none and the eleventh heart does nothing. Never shown is
    /// <see cref="double.PositiveInfinity"/> — nothing is more overdue than a
    /// dream nobody has seen — though <see cref="From"/> answers that case
    /// itself, at random, before it asks (D25).
    ///
    /// Whole days as the device counts days, not hours elapsed: the phone works
    /// this out too, and the two must not be able to disagree because their
    /// clocks are a few seconds apart (D36).
    /// </summary>
    public static double FuelOf(Dream dream, DateTimeOffset utcNow, int utcOffsetMinutes)
    {
        if (dream.LastShownAt is not { } shown) return double.PositiveInfinity;

        var days = Math.Max(
            0,
            LocalDate(utcNow, utcOffsetMinutes).DayNumber - LocalDate(shown, utcOffsetMinutes).DayNumber);

        return days * (1 + Math.Clamp(dream.Likes, 0, LikesCap) / (double)LikesCap);
    }

    private static DateOnly LocalDate(DateTimeOffset instant, int utcOffsetMinutes) =>
        DateOnly.FromDateTime(NudgeSchedule.LocalNow(instant, utcOffsetMinutes));
}
