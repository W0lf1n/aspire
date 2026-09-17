using Aspire.Domain;
using Aspire.Infrastructure;
using Aspire.Infrastructure.Media;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Dreams;

/// <summary>
/// The dreams of one board. Every method takes the board first, and a dream
/// on another board is simply not found: the board id comes from the
/// device's token, never from the request, so there is nothing to forge.
///
/// Validation speaks Czech because its sentences go straight to the screen,
/// under the field the person was typing in. The client says the same
/// things in the same order (<c>rules.ts</c>), so the server's are a backstop.
/// </summary>
public sealed class DreamService(AppDbContext db, MediaStore media)
{
    public const int TargetYearMin = 2000;
    public const int TargetYearMax = 2100;

    public Task<List<Dream>> ListAsync(string boardId, CancellationToken ct = default) =>
        // The tie-break is the id, not `CreatedAt`: SQLite cannot order by a
        // DateTimeOffset, and the laptop mode runs the same query.
        db.Dreams
            .Where(d => d.BoardId == boardId)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Id)
            .ToListAsync(ct);

    public Task<Dream?> FindAsync(string boardId, Guid id, CancellationToken ct = default) =>
        db.Dreams.FirstOrDefaultAsync(d => d.BoardId == boardId && d.Id == id, ct);

    /// <summary>The sentence for a bad input, or null when it is fine.</summary>
    public static string? Problem(DreamInput input)
    {
        var title = input.Title?.Trim() ?? string.Empty;
        if (title.Length == 0) return "Napiš název.";
        if (title.Length > Dream.TitleMaxLength) return $"Název má nejvýš {Dream.TitleMaxLength} znaků.";

        var why = input.Why?.Trim() ?? string.Empty;
        if (why.Length > Dream.WhyMaxLength) return $"Proč má nejvýš {Dream.WhyMaxLength} znaků.";

        var affirmation = input.Affirmation?.Trim() ?? string.Empty;
        if (affirmation.Length > Dream.AffirmationMaxLength)
        {
            return $"Afirmace má nejvýš {Dream.AffirmationMaxLength} znaků.";
        }

        if (input.TargetYear is { } year && (year < TargetYearMin || year > TargetYearMax))
        {
            return $"Rok napiš mezi {TargetYearMin} a {TargetYearMax}.";
        }

        return null;
    }

    public async Task<Dream> CreateAsync(string boardId, DreamInput input, CancellationToken ct = default)
    {
        // In front of everything already there: a new dream is number one in
        // the Seznam, which is where the person writing it is looking (D44,
        // D79). Below zero is fine — it is a key to order by, and the next
        // `PlaceAsync` counts the board off from nought again.
        var first = await db.Dreams
            .Where(d => d.BoardId == boardId)
            .MinAsync(d => (int?)d.SortOrder, ct);

        var now = DateTimeOffset.UtcNow;
        var dream = new Dream
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Title = string.Empty,
            SortOrder = (first ?? 1) - 1,
            CreatedAt = now
        };
        Apply(dream, input, now);

        db.Dreams.Add(dream);
        await db.SaveChangesAsync(ct);
        return dream;
    }

    public async Task<Dream?> UpdateAsync(string boardId, Guid id, DreamInput input, CancellationToken ct = default)
    {
        var dream = await FindAsync(boardId, id, ct);
        if (dream is null) return null;

        Apply(dream, input, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return dream;
    }

    public async Task<bool> DeleteAsync(string boardId, Guid id, CancellationToken ct = default)
    {
        var dream = await FindAsync(boardId, id, ct);
        if (dream is null) return false;

        // The rows go by cascade; the photographs on disk are ours to remove.
        db.Dreams.Remove(dream);
        await db.SaveChangesAsync(ct);
        media.DeleteDream(dream.Id);
        return true;
    }

    /// <summary>
    /// Which collage template the tile uses (D82). It changes what the dream
    /// looks like, so it moves <c>UpdatedAt</c> the way a photograph does.
    /// Null is no such dream; the sentence is a variant there is no such thing
    /// as.
    /// </summary>
    public async Task<(Dream? Dream, string? Problem)> LayoutAsync(
        string boardId, Guid id, int layout, CancellationToken ct = default)
    {
        if (layout < 0 || layout >= Dream.LayoutsMax) return (null, "Takové rozložení není.");

        var dream = await FindAsync(boardId, id, ct);
        if (dream is null) return (null, null);

        dream.Layout = layout;
        dream.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return (dream, null);
    }

    /// <summary>
    /// The collage and each photograph, or the photographs alone (D87). Like
    /// the template it is how the dream looks, so it moves <c>UpdatedAt</c>.
    /// Null is no such dream.
    /// </summary>
    public async Task<Dream?> ViewAsync(
        string boardId, Guid id, PhotoView view, CancellationToken ct = default)
    {
        var dream = await FindAsync(boardId, id, ct);
        if (dream is null) return null;

        dream.PhotoView = view;
        dream.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return dream;
    }

    /// <summary>
    /// Every dream on the board, and every photograph with them: starting
    /// over (D80). How many dreams went comes back, for the sentence that says
    /// so.
    ///
    /// The board itself stays, and so does everything that is about the board
    /// rather than about a dream — its devices, their nudges, its lock-screen
    /// link. The rows of the photographs and the share links go by cascade and
    /// by being columns of what is deleted; the files are ours to remove, and
    /// so is an upload still waiting in the temp directory for the worker.
    /// </summary>
    public async Task<int> ResetAsync(string boardId, CancellationToken ct = default)
    {
        var rows = await db.Dreams.Where(d => d.BoardId == boardId).ToListAsync(ct);
        if (rows.Count == 0) return 0;

        var ids = rows.Select(d => d.Id).ToList();
        var staged = await db.DreamImages
            .Where(i => ids.Contains(i.DreamId) && i.ProcessedAt == null)
            .Select(i => i.Id)
            .ToListAsync(ct);

        db.Dreams.RemoveRange(rows);
        await db.SaveChangesAsync(ct);

        foreach (var id in ids) media.DeleteDream(id);
        foreach (var id in staged) File.Delete(Images.ImageService.StagedPath(id));
        return rows.Count;
    }

    /// <summary>
    /// This dream to that line of the Seznam, counted from one, and everything
    /// between where it was and where it is now moves over by one (D79).
    ///
    /// The whole board is counted off again, 0…N−1, rather than a gap being
    /// found between two neighbours: a hundred rows is nothing, and an order
    /// that is always whole numbers from nought is an order with no drift to
    /// tidy up later. A place past either end is the end. `UpdatedAt` is left
    /// alone — where a dream stands in a list is about the list (D77).
    /// </summary>
    public async Task<bool> PlaceAsync(string boardId, Guid id, int place, CancellationToken ct = default)
    {
        var rows = await ListAsync(boardId, ct);
        var dream = rows.FirstOrDefault(d => d.Id == id);
        if (dream is null) return false;

        rows.Remove(dream);
        rows.Insert(Math.Clamp(place - 1, 0, rows.Count), dream);
        for (var i = 0; i < rows.Count; i++) rows[i].SortOrder = i;

        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// One more. Counted in the database rather than read-add-write, so two
    /// taps from two devices at the same moment both land.
    /// </summary>
    public async Task<Dream?> LikeAsync(string boardId, Guid id, CancellationToken ct = default)
    {
        var touched = await db.Dreams
            .Where(d => d.BoardId == boardId && d.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.Likes, d => d.Likes + 1), ct);
        if (touched == 0) return null;

        return await db.Dreams.AsNoTracking().FirstAsync(d => d.Id == id, ct);
    }

    /// <summary>
    /// This dream was the board's first tile today. The daily pick reads the
    /// stamp back: least recently shown first, so tomorrow the board opens
    /// on another one and every device on the board agrees on which.
    /// </summary>
    public async Task<bool> MarkShownAsync(string boardId, Guid id, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var touched = await db.Dreams
            .Where(d => d.BoardId == boardId && d.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(d => d.LastShownAt, now), ct);
        return touched > 0;
    }

    // ── Teď, the second reel (D53) ──────────────────────────────────────────

    /// <summary>The dreams on Teď, in the order the person put them in.</summary>
    public Task<List<Dream>> FocusAsync(string boardId, CancellationToken ct = default) =>
        db.Dreams
            .Where(d => d.BoardId == boardId && d.FocusRank != null)
            // The id breaks a tie, as everywhere else: two dreams can share a
            // rank and the order still has to be the same on every device.
            .OrderBy(d => d.FocusRank)
            .ThenBy(d => d.Id)
            .ToListAsync(ct);

    /// <summary>
    /// Put a dream on Teď, behind the ones already there.
    ///
    /// Both null is no such dream on this board. A sentence is a refusal the
    /// screen says out loud. Asking for one that is already on Teď is the
    /// dream back unchanged: the pill is a state, and tapping it twice from
    /// two devices must not make it an error.
    /// </summary>
    public async Task<(Dream? Dream, string? Problem)> AddToFocusAsync(
        string boardId, Guid id, CancellationToken ct = default)
    {
        var dream = await FindAsync(boardId, id, ct);
        if (dream is null) return (null, null);
        if (dream.FocusRank is not null) return (dream, null);

        // Teď is what is being worked on now, and the wall is what is behind
        // you. A dream cannot be both.
        if (dream.Status == DreamStatus.Achieved) return (null, "Splněný sen na teď nepatří.");

        var count = await db.Dreams.CountAsync(d => d.BoardId == boardId && d.FocusRank != null, ct);
        if (count >= Dream.FocusMax)
        {
            return (null, $"Na teď máš už {Dream.FocusMax} snů. Některý nejdřív odeber.");
        }

        var last = await db.Dreams
            .Where(d => d.BoardId == boardId && d.FocusRank != null)
            .MaxAsync(d => (int?)d.FocusRank, ct);

        // `UpdatedAt` stays where it was, here and in the two below: which reel
        // a dream is on says something about the board and nothing about the
        // dream, and the date on its screen means „I last changed this“ (D77).
        dream.FocusRank = (last ?? 0) + 1;
        await db.SaveChangesAsync(ct);
        return (dream, null);
    }

    /// <summary>
    /// Take a dream off Teď. True when there is such a dream, whether or not
    /// it was on it — taking off what is already off is not a failure.
    /// </summary>
    public async Task<bool> RemoveFromFocusAsync(string boardId, Guid id, CancellationToken ct = default)
    {
        var dream = await FindAsync(boardId, id, ct);
        if (dream is null) return false;
        if (dream.FocusRank is null) return true;

        dream.FocusRank = null;
        await db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// The whole of Teď, in one request: these dreams, in this order, and
    /// nothing else on it.
    ///
    /// One call rather than a move-up and a move-down each, so the ten are
    /// never half-ordered on the server — the screen sends what it has when
    /// the person is done, and an empty list is a Teď emptied on purpose.
    /// </summary>
    public async Task<(List<Dream>? Dreams, string? Problem)> ReorderFocusAsync(
        string boardId, IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count > Dream.FocusMax)
        {
            return (null, $"Na teď se vejde nejvýš {Dream.FocusMax} snů.");
        }

        if (ids.Distinct().Count() != ids.Count) return (null, "Jeden sen tam nemůže být dvakrát.");

        var asked = await db.Dreams
            .Where(d => d.BoardId == boardId && ids.Contains(d.Id))
            .ToListAsync(ct);
        // A dream on another board is simply not among them, so this is also
        // what refuses one: the count comes up short.
        if (asked.Count != ids.Count) return (null, "Některý z těch snů na nástěnce není.");
        if (asked.Any(d => d.Status == DreamStatus.Achieved))
        {
            return (null, "Splněný sen na teď nepatří.");
        }

        // Everything that was on Teď leaves it first, so a dream left out of
        // the new order is off it. These are the same tracked instances the
        // query above returned, so the ranks below land on top.
        foreach (var dream in await db.Dreams
            .Where(d => d.BoardId == boardId && d.FocusRank != null)
            .ToListAsync(ct))
        {
            dream.FocusRank = null;
        }

        var byId = asked.ToDictionary(d => d.Id);
        for (var i = 0; i < ids.Count; i++)
        {
            byId[ids[i]].FocusRank = i + 1;
        }

        await db.SaveChangesAsync(ct);
        return (await FocusAsync(boardId, ct), null);
    }

    /// <summary>
    /// Validated input onto a dream. Achieved gets its date the first time
    /// it is set and keeps it after; leaving achieved gives the date back.
    /// </summary>
    private static void Apply(Dream dream, DreamInput input, DateTimeOffset now)
    {
        dream.Title = input.Title!.Trim();
        dream.Why = input.Why?.Trim() ?? string.Empty;
        dream.Affirmation = input.Affirmation?.Trim() ?? string.Empty;
        dream.TargetYear = input.TargetYear;
        dream.Status = input.Status ?? DreamStatus.Dreaming;
        dream.Category = input.Category;
        dream.AchievedAt = dream.Status == DreamStatus.Achieved ? dream.AchievedAt ?? now : null;
        // A dream marked splněno leaves Teď the way it leaves the reel: it is
        // behind you now, and the slot it held is free (D53).
        if (dream.Status == DreamStatus.Achieved) dream.FocusRank = null;
        dream.UpdatedAt = now;
    }
}
