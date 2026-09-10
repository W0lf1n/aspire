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
    /// Whether this subscription is due. <paramref name="lastSentOn"/> is the
    /// local date it was last nudged, which is what keeps a day to one.
    /// </summary>
    public static bool IsDue(NudgeMode mode, int atMinutes, DateTime localNow, DateOnly? lastSentOn)
    {
        if (mode == NudgeMode.Off) return false;

        var today = DateOnly.FromDateTime(localNow);
        if (lastSentOn == today) return false;

        if (mode == NudgeMode.Weekdays &&
            localNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return false;
        }

        var minutes = localNow.Hour * 60 + localNow.Minute;
        return minutes >= atMinutes && minutes < atMinutes + GraceMinutes;
    }

    /// <summary>A time of day the device may ask for: any minute of one.</summary>
    public static bool IsTimeOfDay(int atMinutes) => atMinutes is >= 0 and < 24 * 60;

    /// <summary>
    /// An offset a real place has. Fourteen hours ahead is Kiritimati and
    /// twelve behind is Baker Island; anything outside that is a broken
    /// clock, not a timezone.
    /// </summary>
    public static bool IsUtcOffset(int minutes) => minutes is >= -12 * 60 and <= 14 * 60;
}
