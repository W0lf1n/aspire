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
        // Behind everything already there: a new dream joins at the end.
        var last = await db.Dreams
            .Where(d => d.BoardId == boardId)
            .MaxAsync(d => (int?)d.SortOrder, ct);

        var now = DateTimeOffset.UtcNow;
        var dream = new Dream
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            Title = string.Empty,
            SortOrder = (last ?? -1) + 1,
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
        dream.AchievedAt = dream.Status == DreamStatus.Achieved ? dream.AchievedAt ?? now : null;
        dream.UpdatedAt = now;
    }
}
