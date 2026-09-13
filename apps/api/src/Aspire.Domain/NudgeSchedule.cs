namespace Aspire.Domain;

/// <summary>
/// Whether a device is owed its morning nudge right now (PLAN.md §3.6).
///
/// Pure: it takes the subscription's own numbers and the current instant and
/// answers yes or no, so every edge of it — a weekend, a day already sent, a
/// server that was down all morning — is a test rather than a thing you wait
/// a day to find out.
/// </summary>
public static class NudgeSchedule
{
    /// <summary>
    /// How late a nudge may still arrive. A worker that was stopped over
    /// breakfast should still send at ten past; one that starts at eleven at
    /// night should not, because a dream at bedtime is not the morning
    /// habit this is (§3.6) and would be the app's first ever unwelcome
    /// notification. Past the window the day is simply skipped.
    /// </summary>
    public const int GraceMinutes = 120;

    /// <summary>The device's own wall clock, from its offset.</summary>
    public static DateTime LocalNow(DateTimeOffset utcNow, int utcOffsetMinutes) =>
        utcNow.UtcDateTime.AddMinutes(utcOffsetMinutes);

    /// <summary>
    /// Which of this device's reminders it is owed right now, as minutes past
    /// local midnight — or null when it is owed none (D72).
    ///
    /// <paramref name="lastSentOn"/> and <paramref name="lastSentMinutes"/>
    /// together say how far through today this device has been taken: the
    /// times are in order, so everything up to and including that minute is
    /// done and everything after it is not. A stamp from another day says
    /// nothing about this one.
    ///
    /// **The latest one that is due wins.** A server that was down over
    /// breakfast comes back to two reminders inside their grace and sends the
    /// most recent, not both and not the oldest: the point of the thing is a
    /// dream now, and marking it sent takes the one it overtook with it.
    /// </summary>
    public static int? DueAt(
        NudgeMode mode,
        IReadOnlyList<int> times,
        DateTime localNow,
        DateOnly? lastSentOn,
        int? lastSentMinutes)
    {
        if (mode == NudgeMode.Off) return null;

        if (mode == NudgeMode.Weekdays &&
            localNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return null;
        }

        var minutes = localNow.Hour * 60 + localNow.Minute;
        var sent = lastSentOn == DateOnly.FromDateTime(localNow) ? lastSentMinutes : null;

        int? owed = null;
        foreach (var at in times)
        {
            // Due now, and not so long ago that a dream would be a surprise.
            if (minutes < at || minutes >= at + GraceMinutes) continue;
            // Today has already been taken past this one.
            if (sent is { } last && at <= last) continue;
            if (owed is null || at > owed) owed = at;
        }

        return owed;
    }

    /// <summary>
    /// A set of times a device may ask for: one to
    /// <see cref="PushSubscription.MaxTimes"/>, each a time of day, and no
    /// two the same. The order is <see cref="Tidy"/>'s to impose.
    /// </summary>
    public static bool AreTimesOfDay(IReadOnlyList<int>? times) =>
        times is { Count: >= 1 } &&
        times.Count <= PushSubscription.MaxTimes &&
        times.All(IsTimeOfDay) &&
        times.Distinct().Count() == times.Count;

    /// <summary>
    /// The times as they are stored: in order, with repeats dropped. Sorted
    /// because <see cref="DueAt"/>'s „everything up to here is done“ is only
    /// true of a list that is in order, and because two devices asking for
    /// the same set in a different order are asking for the same thing.
    /// </summary>
    public static List<int> Tidy(IEnumerable<int> times) =>
        times.Distinct().Order().ToList();

    /// <summary>A time of day the device may ask for: any minute of one.</summary>
    public static bool IsTimeOfDay(int atMinutes) => atMinutes is >= 0 and < 24 * 60;

    /// <summary>
    /// An offset a real place has. Fourteen hours ahead is Kiritimati and
    /// twelve behind is Baker Island; anything outside that is a broken
    /// clock, not a timezone.
    /// </summary>
    public static bool IsUtcOffset(int minutes) => minutes is >= -12 * 60 and <= 14 * 60;
}
