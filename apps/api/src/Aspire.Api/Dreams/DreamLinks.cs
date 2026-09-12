using Aspire.Api.Auth;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Dreams;

/// <summary>
/// One dream's share link (PLAN.md §3.7, D61): the same key as the board's
/// lock-screen link (<c>BoardLinks</c>, D60), against one dream.
///
/// The whole point is that the other person has nothing — no app, no code, no
/// account — and gets one dream. So the key opens a page with that dream's
/// photograph and its title on it, and there is no route from it to a second
/// dream, to the board, or to anything that writes.
///
/// Every method takes the board as well as the dream, because the board comes
/// from the device's token and a dream on another board must simply not be
/// found — the same rule the rest of <c>DreamService</c> keeps.
/// </summary>
public sealed class DreamLinks(AppDbContext db)
{
    /// <summary>
    /// Where a shared dream is read. Short, and outside <c>/api/</c>, because
    /// this is the one URL in the app a person reads off a screen and pastes
    /// into a message.
    /// </summary>
    public static string PathOf(string key) => $"/s/{key}";

    public Task<string?> KeyOfAsync(string boardId, Guid dreamId, CancellationToken ct = default) =>
        db.Dreams
            .AsNoTracking()
            .Where(d => d.BoardId == boardId && d.Id == dreamId)
            .Select(d => d.LinkKey)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// A new key for this dream, replacing whatever it had — so making one is
    /// also how an old link is revoked. Null when the dream is not this
    /// board's, which is the same as not existing.
    /// </summary>
    public async Task<string?> MakeAsync(string boardId, Guid dreamId, CancellationToken ct = default)
    {
        var key = ShareKey.New();

        var touched = await db.Dreams
            .Where(d => d.BoardId == boardId && d.Id == dreamId)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.LinkKey, key), ct);

        return touched > 0 ? key : null;
    }

    /// <summary>The link stops working. A dream with no link is not a failure.</summary>
    public Task RevokeAsync(string boardId, Guid dreamId, CancellationToken ct = default) =>
        db.Dreams
            .Where(d => d.BoardId == boardId && d.Id == dreamId)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.LinkKey, (string?)null), ct);

    /// <summary>
    /// The dream a key opens, or none. Untracked and by the unique index:
    /// nothing is compared here, so there is no timing to leak — and 32 random
    /// bytes leave nothing worth timing anyway.
    /// </summary>
    public Task<Dream?> FindAsync(string key, CancellationToken ct = default)
    {
        if (!ShareKey.CouldBe(key)) return Task.FromResult<Dream?>(null);

        return db.Dreams
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.LinkKey == key, ct);
    }
}
