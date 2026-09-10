using Aspire.Api.Auth;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Boards;

/// <summary>
/// The first board, from configuration.
///
/// <c>Pairing:Code</c> is Prosper's one setting and the runbook still asks for
/// it. On first start it becomes the code of the first board, and from then on
/// the code lives in the database, where <c>board code</c> can change it. A
/// board that has no code yet — the one the Boards migration made for rows
/// that predate boards — takes the configured code as well, so the setting
/// never sits unused while a board cannot pair.
/// </summary>
public static class BoardSeed
{
    public const string FirstBoardName = "Nástěnka";

    /// <returns>The board that received the code, or null if none did.</returns>
    public static async Task<Board?> ApplyAsync(AppDbContext db, string? code, CancellationToken ct = default)
    {
        code = code?.Trim() ?? string.Empty;
        var hasCode = code.Length > 0;

        if (!await db.Boards.AnyAsync(ct))
        {
            var first = new Board
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = FirstBoardName,
                CodeHash = hasCode ? PairingCode.Hash(code) : string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.Boards.Add(first);
            await db.SaveChangesAsync(ct);
            return hasCode ? first : null;
        }

        if (!hasCode) return null;

        // The id, not `CreatedAt`: SQLite cannot order by a DateTimeOffset.
        var unset = await db.Boards
            .Where(b => b.CodeHash == string.Empty)
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync(ct);
        if (unset is null) return null;

        unset.CodeHash = PairingCode.Hash(code);
        await db.SaveChangesAsync(ct);
        return unset;
    }
}
