using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Nudges;

/// <summary>
/// The devices that want a dream in the morning (PLAN.md §3.6), per board.
///
/// A subscription belongs to the board its device paired into, and the board
/// id comes from the token, never from the request — the same rule the
/// dreams follow. Turning the nudge off deletes the row: there is no such
/// thing as a subscription kept for later, and a browser that revokes one
/// leaves nothing behind either.
/// </summary>
public sealed class NudgeService(AppDbContext db)
{
    /// <summary>The sentence for a bad input, or null when it is fine.</summary>
    public static string? Problem(NudgeInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Endpoint)) return "Prohlížeč nedal adresu pro upozornění.";
        if (input.Endpoint.Length > PushSubscription.EndpointMaxLength)
        {
            return "Adresa pro upozornění je moc dlouhá.";
        }

        if (string.IsNullOrWhiteSpace(input.P256dh) || string.IsNullOrWhiteSpace(input.Auth))
        {
            return "Prohlížeč nedal klíče pro upozornění.";
        }

        if (input.P256dh.Length > PushSubscription.KeyMaxLength ||
            input.Auth.Length > PushSubscription.KeyMaxLength)
        {
            return "Klíče pro upozornění jsou moc dlouhé.";
        }

        if (input.Times is { } times && !NudgeSchedule.AreTimesOfDay(times))
        {
            return times.Count > PushSubscription.MaxTimes
                ? $"Víc než {PushSubscription.MaxTimes} připomenutí denně nejde."
                : "Tenhle čas neznám.";
        }

        if (input.UtcOffsetMinutes is { } offset && !NudgeSchedule.IsUtcOffset(offset))
        {
            return "Tohle časové pásmo neznám.";
        }

        return null;
    }

    /// <summary>The sentence for a bad offset report, or null when it is fine.</summary>
    public static string? Problem(NudgeOffsetInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Endpoint)) return "Prohlížeč nedal adresu pro upozornění.";
        if (input.Endpoint.Length > PushSubscription.EndpointMaxLength)
        {
            return "Adresa pro upozornění je moc dlouhá.";
        }

        if (input.UtcOffsetMinutes is null || !NudgeSchedule.IsUtcOffset(input.UtcOffsetMinutes.Value))
        {
            return "Tohle časové pásmo neznám.";
        }

        return null;
    }

    /// <summary>
    /// This device's subscription as it stands, for reading only.
    ///
    /// Untracked, for <c>DueAsync</c>'s reason (D34): <c>MarkSentAsync</c> and
    /// <c>UpdateOffsetAsync</c> both write with <c>ExecuteUpdate</c>, which
    /// goes round the change tracker, so a tracked read after one of them
    /// hands back the row as it was before. Nothing mutates what comes out of
    /// here — <c>SaveAsync</c> does its own tracked query for that.
    /// </summary>
    public Task<PushSubscription?> FindAsync(string boardId, string endpoint, CancellationToken ct = default) =>
        db.PushSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BoardId == boardId && s.Endpoint == endpoint, ct);

    /// <summary>
    /// The device's subscription, made or brought up to date. The endpoint is
    /// the device, so opening the app again with the same one moves the time
    /// and the offset rather than leaving a second row to nudge from.
    /// </summary>
    public async Task<PushSubscription> SaveAsync(
        string boardId, NudgeInput input, CancellationToken ct = default)
    {
        var endpoint = input.Endpoint!.Trim();

        // Looked up by endpoint alone: a phone re-paired into another board
        // keeps its endpoint, and it must move rather than collide with the
        // unique index.
        var subscription = await db.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);
        if (subscription is null)
        {
            subscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                BoardId = boardId,
                Endpoint = endpoint,
                P256dh = string.Empty,
                Auth = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.PushSubscriptions.Add(subscription);
        }

        subscription.BoardId = boardId;
        subscription.P256dh = input.P256dh!.Trim();
        subscription.Auth = input.Auth!.Trim();
        subscription.Mode = input.Mode ?? NudgeMode.Daily;
        // In order and without repeats, whatever order the screen sent them
        // in: `DueAt` reads the list as „everything up to here is done“ (D72).
        subscription.Times = input.Times is { Count: > 0 } asked
            ? NudgeSchedule.Tidy(asked)
            : [PushSubscription.DefaultAtMinutes];
        subscription.UtcOffsetMinutes = input.UtcOffsetMinutes ?? 0;

        await db.SaveChangesAsync(ct);
        return subscription;
    }

    /// <summary>
    /// This device has moved, or the clocks have: the offset brought up to
    /// date and nothing else touched (D51).
    ///
    /// D34 promised the device would say where it is every time the app
    /// opens, and until now only moving the switch in Upozornění said it — so
    /// a subscription made in summer nudged an hour out all winter. False
    /// when this board has no such subscription, which is not a failure: a
    /// device that has never asked to be nudged has no offset to keep.
    /// </summary>
    public async Task<bool> UpdateOffsetAsync(
        string boardId, string endpoint, int utcOffsetMinutes, CancellationToken ct = default)
    {
        var changed = await db.PushSubscriptions
            .Where(s => s.BoardId == boardId && s.Endpoint == endpoint)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.UtcOffsetMinutes, utcOffsetMinutes), ct);
        return changed > 0;
    }

    /// <summary>Off is gone: the row goes, and so does anything to send to it.</summary>
    public async Task<bool> RemoveAsync(string boardId, string endpoint, CancellationToken ct = default)
    {
        var removed = await db.PushSubscriptions
            .Where(s => s.BoardId == boardId && s.Endpoint == endpoint)
            .ExecuteDeleteAsync(ct);
        return removed > 0;
    }

    /// <summary>
    /// Every subscription that is owed a nudge at this instant, across every
    /// board. The worker's one query: the schedule is worked out here rather
    /// than in SQL, because it is a rule with a test and SQLite cannot do the
    /// arithmetic on a DateTimeOffset anyway.
    ///
    /// Untracked, and it has to be. <c>MarkSentAsync</c> writes with
    /// <c>ExecuteUpdate</c>, which goes round the change tracker; a tracked
    /// read afterwards would hand back the row as it was before the stamp and
    /// nudge the same phone twice.
    /// </summary>
    public async Task<List<Due>> DueAsync(DateTimeOffset utcNow, CancellationToken ct = default)
    {
        var candidates = await db.PushSubscriptions
            .AsNoTracking()
            .Where(s => s.Mode != NudgeMode.Off)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        var due = new List<Due>();
        foreach (var subscription in candidates)
        {
            var at = NudgeSchedule.DueAt(
                subscription.Mode,
                subscription.Times,
                NudgeSchedule.LocalNow(utcNow, subscription.UtcOffsetMinutes),
                subscription.LastSentOn,
                subscription.LastSentMinutes);

            if (at is { } owed) due.Add(new Due(subscription, owed));
        }

        return due;
    }

    /// <summary>
    /// This device has had the reminder that was owed at
    /// <paramref name="atMinutes"/>; not that one or any earlier one again
    /// today (D72).
    /// </summary>
    public async Task MarkSentAsync(
        PushSubscription subscription, int atMinutes, DateTimeOffset utcNow, CancellationToken ct = default)
    {
        var local = NudgeSchedule.LocalNow(utcNow, subscription.UtcOffsetMinutes);
        await db.PushSubscriptions
            .Where(s => s.Id == subscription.Id)
            .ExecuteUpdateAsync(
                set => set
                    .SetProperty(s => s.LastSentOn, DateOnly.FromDateTime(local))
                    .SetProperty(s => s.LastSentMinutes, atMinutes),
                ct);
    }

    /// <summary>
    /// A push service that says the subscription is gone (404 or 410) is
    /// telling the truth: the browser dropped it. Keeping the row would mean
    /// trying again every morning forever.
    /// </summary>
    public Task ForgetAsync(Guid id, CancellationToken ct = default) =>
        db.PushSubscriptions.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
}

/// <summary>A device owed a nudge, and which of its reminders it is owed (D72).</summary>
public readonly record struct Due(PushSubscription Subscription, int AtMinutes);
