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

        if (input.AtMinutes is { } at && !NudgeSchedule.IsTimeOfDay(at)) return "Tenhle čas neznám.";

        if (input.UtcOffsetMinutes is { } offset && !NudgeSchedule.IsUtcOffset(offset))
        {
            return "Tohle časové pásmo neznám.";
        }

        return null;
    }

    public Task<PushSubscription?> FindAsync(string boardId, string endpoint, CancellationToken ct = default) =>
        db.PushSubscriptions.FirstOrDefaultAsync(s => s.BoardId == boardId && s.Endpoint == endpoint, ct);

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
        subscription.AtMinutes = input.AtMinutes ?? PushSubscription.DefaultAtMinutes;
        subscription.UtcOffsetMinutes = input.UtcOffsetMinutes ?? 0;

        await db.SaveChangesAsync(ct);
        return subscription;
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
    public async Task<List<PushSubscription>> DueAsync(DateTimeOffset utcNow, CancellationToken ct = default)
    {
        var candidates = await db.PushSubscriptions
            .AsNoTracking()
            .Where(s => s.Mode != NudgeMode.Off)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        return candidates
            .Where(s => NudgeSchedule.IsDue(
                s.Mode,
                s.AtMinutes,
                NudgeSchedule.LocalNow(utcNow, s.UtcOffsetMinutes),
                s.LastSentOn))
            .ToList();
    }

    /// <summary>This device has had today's nudge; not again until tomorrow.</summary>
    public async Task MarkSentAsync(PushSubscription subscription, DateTimeOffset utcNow, CancellationToken ct = default)
    {
        var local = NudgeSchedule.LocalNow(utcNow, subscription.UtcOffsetMinutes);
        await db.PushSubscriptions
            .Where(s => s.Id == subscription.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(s => s.LastSentOn, DateOnly.FromDateTime(local)), ct);
    }

    /// <summary>
    /// A push service that says the subscription is gone (404 or 410) is
    /// telling the truth: the browser dropped it. Keeping the row would mean
    /// trying again every morning forever.
    /// </summary>
    public Task ForgetAsync(Guid id, CancellationToken ct = default) =>
        db.PushSubscriptions.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
}
