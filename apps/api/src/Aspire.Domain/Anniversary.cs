namespace Aspire.Domain;

/// <summary>
/// The dream that came true on this day in an earlier year (PLAN.md §3.3), on
/// the server's side.
///
/// The board works the same rule out for itself in `dreams/board.ts` and shows
/// it as a line over the reel (D29). This exists for the same reason
/// <see cref="DailyPick"/> does: the morning nudge has to name a dream while
/// the phone is asleep, and on the one morning a year a dream has an
/// anniversary that is what the nudge is about instead of today's dream (D57).
/// </summary>
public static class Anniversary
{
    /// <summary>A dream that came true on this day in an earlier year.</summary>
    public readonly record struct Year(Dream Dream, int Years);

    /// <summary>
    /// Today's anniversary, or none. The day is the device's own, through its
    /// offset, as the pick's is — an anniversary lands on the day the person
    /// is living rather than on UTC's, which matters most in the hours either
    /// side of midnight, when a nudge is never sent anyway but a board is
    /// opened.
    ///
    /// Whole years only, and at least one: a dream achieved this morning is
    /// not an anniversary, it is today. A dream achieved on 29 February has
    /// its anniversary on 29 February, because the alternative is inventing a
    /// date it did not happen on. When two fell on the same day the one
    /// achieved most recently wins, which is the order the wall is in — one
    /// line, never a list.
    /// </summary>
    public static Year? Today(
        IReadOnlyList<Dream> dreams,
        DateTimeOffset utcNow,
        int utcOffsetMinutes)
    {
        var today = NudgeSchedule.LocalNow(utcNow, utcOffsetMinutes);

        var candidates = dreams
            .Where(d => d.Status == DreamStatus.Achieved && d.AchievedAt is not null)
            .OrderByDescending(d => d.AchievedAt!.Value)
            .ThenBy(d => d.SortOrder);

        foreach (var dream in candidates)
        {
            var then = NudgeSchedule.LocalNow(dream.AchievedAt!.Value, utcOffsetMinutes);
            if (then.Month != today.Month || then.Day != today.Day) continue;

            var years = today.Year - then.Year;
            if (years >= 1) return new Year(dream, years);
        }

        return null;
    }
}
