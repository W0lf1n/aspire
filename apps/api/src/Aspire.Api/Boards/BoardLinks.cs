using Aspire.Api.Auth;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Boards;

/// <summary>
/// The board's lock-screen link: the key that is its own permission (D60).
///
/// A morning automation on the phone fetches a URL and sets the picture from
/// it; there is nowhere in that to put a bearer token that a person would not
/// have to type by hand, and a token typed by hand is a token out of the app
/// and a typo away from a wallpaper that silently never changes. So the link
/// carries its own key: whoever holds it holds one collage of this board and
/// nothing else — no dreams to read, no writes, no token — and one tap makes a
/// new one, which is what revoking looks like.
/// </summary>
public sealed class BoardLinks(AppDbContext db)
{
    /// <summary>Where a key is fetched from. The client makes the URL out of it.</summary>
    public static string PathOf(string key) => $"/api/v1/w/{key}";

    public Task<string?> KeyOfAsync(string boardId, CancellationToken ct = default) =>
        db.Boards
            .AsNoTracking()
            .Where(b => b.Id == boardId)
            .Select(b => b.LinkKey)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// A new key for this board, replacing whatever it had — so making one is
    /// also how an old link is revoked, and there is only ever one to keep
    /// track of.
    /// </summary>
    public async Task<string> MakeAsync(string boardId, CancellationToken ct = default)
    {
        var key = ShareKey.New();

        await db.Boards
            .Where(b => b.Id == boardId)
            .ExecuteUpdateAsync(set => set.SetProperty(b => b.LinkKey, key), ct);

        return key;
    }

    /// <summary>The link stops working. A board with none is not a failure.</summary>
    public Task RevokeAsync(string boardId, CancellationToken ct = default) =>
        db.Boards
            .Where(b => b.Id == boardId)
            .ExecuteUpdateAsync(set => set.SetProperty(b => b.LinkKey, (string?)null), ct);

    /// <summary>
    /// Whose board a key is, or none. Untracked, and by the unique index:
    /// nothing is compared here, so there is no timing to leak — and 32 random
    /// bytes leave nothing worth timing anyway.
    /// </summary>
    public Task<string?> BoardOfAsync(string key, CancellationToken ct = default)
    {
        if (!ShareKey.CouldBe(key)) return Task.FromResult<string?>(null);

        return db.Boards
            .AsNoTracking()
            .Where(b => b.LinkKey == key)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(ct);
    }
}
