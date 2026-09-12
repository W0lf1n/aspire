namespace Aspire.Domain;

/// <summary>
/// The dreams on a lock screen nobody chose by hand (PLAN.md §3.5), the twin
/// of <c>wallpaperPick</c> in `dreams/wallpaper.ts`.
///
/// It exists for the same reason <see cref="DailyPick"/> does: the morning
/// automation fetches the collage through a link while the phone is asleep and
/// there is nobody to work it out on the device (D60). Written in §22.2's
/// shape but built here, with the endpoint that asks for it — a rule with no
/// caller is a rule that drifts from its twin without anybody noticing (D58).
/// </summary>
public static class WallpaperPick
{
    /// <summary>
    /// The day's dream first when it has a photograph, then the reel's dreams
    /// with the most fuel — how long each has waited, weighted by its hearts
    /// (D58). Fixed order, no shuffle: a lock screen that reshuffled between
    /// two fetches of the same morning would be two different wallpapers.
    ///
    /// The wall is left out, though the choice by hand keeps it: a dream
    /// already lived is worth looking at, but what is automatic should be what
    /// is still ahead, because that is what a lock screen is for (D59).
    /// </summary>
    /// <param name="photographed">
    /// The dreams whose dreamt photograph is ready. It is passed in because a
    /// dream on this side does not carry its images, and the endpoint has
    /// already read them.
    /// </param>
    public static List<Dream> From(
        IReadOnlyList<Dream> dreams,
        IReadOnlySet<Guid> photographed,
        Guid? pickedId,
        DateTimeOffset utcNow,
        int utcOffsetMinutes,
        int count)
    {
        if (count <= 0) return [];

        var candidates = dreams
            .Where(d => d.Status != DreamStatus.Achieved && photographed.Contains(d.Id))
            .ToList();

        var picked = candidates.FirstOrDefault(d => d.Id == pickedId);

        var rest = candidates
            .Where(d => d != picked)
            .OrderByDescending(d => DailyPick.FuelOf(d, utcNow, utcOffsetMinutes))
            // Two dreams nobody has seen are both infinitely overdue, so they
            // fall to board order rather than to an order nothing decides.
            .ThenBy(d => d.SortOrder);

        var chosen = picked is null ? rest.ToList() : [picked, .. rest];
        return chosen.Count <= count ? chosen : chosen[..count];
    }
}
