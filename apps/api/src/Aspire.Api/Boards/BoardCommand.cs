using Aspire.Api.Auth;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Boards;

/// <summary>
/// <c>board invite | add | code | list</c>, run in the API's own image so it
/// has the database and the same hashing. There is no signup form and there
/// is not going to be one (D6): a board is something the operator makes.
///
/// <code>
/// docker compose exec api dotnet Aspire.Api.dll board invite Zuzana
/// docker compose exec api dotnet Aspire.Api.dll board add Zuzana 483920174635
/// docker compose exec api dotnet Aspire.Api.dll board code Nástěnka 209384756123
/// docker compose exec api dotnet Aspire.Api.dll board list
/// </code>
///
/// <c>invite</c> is the one to reach for: it makes the board and the code in
/// the same breath, so the code is the machine's twelve digits rather than
/// whatever a person typed twice (D46). It is printed once and then only the
/// hash exists.
///
/// It speaks English, like the runbook it is part of; the Czech is for the
/// screen.
/// </summary>
public static class BoardCommand
{
    /// <summary>
    /// Six, so the laptop's <c>000000</c> stays legal; the runbook asks for
    /// twelve, and says why.
    /// </summary>
    public const int MinCodeLength = 6;

    /// <summary>
    /// What <c>invite</c> makes: twelve digits, 10^12 of them, which is the
    /// length `DEPLOYMENT.md` has asked a person to produce by hand since the
    /// first board. The fences in front of <c>/api/v1/pair</c> are per client
    /// address and the hash is PBKDF2; the length is the part that does not
    /// depend on either holding.
    /// </summary>
    public const int GeneratedCodeLength = 12;

    private const string Usage =
        "usage: board invite <name> | board add <name> <code> | board code <name> <code> | board list";

    public static bool IsBoardCommand(string[] args) => args.Length > 0 && args[0] == "board";

    /// <returns>The process exit code: 0 done, 1 refused, 2 misused.</returns>
    public static async Task<int> RunAsync(
        string[] args,
        AppDbContext db,
        TextWriter output,
        CancellationToken ct = default)
    {
        switch (args)
        {
            case ["board", "invite", var name]:
                return await AddAsync(db, name, PairingCode.Generate(GeneratedCodeLength), output, ct, say: true);
            case ["board", "add", var name, var code]:
                return await AddAsync(db, name, code, output, ct);
            case ["board", "code", var name, var code]:
                return await SetCodeAsync(db, name, code, output, ct);
            case ["board", "list"]:
                return await ListAsync(db, output, ct);
            default:
                await output.WriteLineAsync(Usage);
                return 2;
        }
    }

    /// <summary>
    /// Digits only, because the field asks the phone for a numeric keypad; a
    /// code with a letter in it cannot be typed on the device that has to type
    /// it. Null when the code is fine.
    /// </summary>
    internal static string? CodeProblem(string code)
    {
        if (code.Length < MinCodeLength) return $"The code needs at least {MinCodeLength} digits.";
        if (!code.All(char.IsAsciiDigit)) return "The code is digits only: the phone shows a numeric keypad.";
        return null;
    }

    /// <param name="say">
    /// Whether the code goes to the screen. It does for <c>invite</c>, which
    /// is the only place the code was never in the operator's hands already —
    /// and it is the one chance to read it, because what is kept is the hash.
    /// </param>
    private static async Task<int> AddAsync(
        AppDbContext db, string name, string code, TextWriter output, CancellationToken ct, bool say = false)
    {
        name = name.Trim();
        if (name.Length == 0 || name.Length > Board.NameMaxLength)
        {
            await output.WriteLineAsync($"A board's name is 1 to {Board.NameMaxLength} characters.");
            return 1;
        }

        if (CodeProblem(code) is { } problem)
        {
            await output.WriteLineAsync(problem);
            return 1;
        }

        if (await db.Boards.AnyAsync(b => b.Name == name, ct))
        {
            await output.WriteLineAsync($"A board called {name} already exists; `board code` changes its code.");
            return 1;
        }

        var board = new Board
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            CodeHash = PairingCode.Hash(code),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.Boards.Add(board);
        await db.SaveChangesAsync(ct);

        if (!say)
        {
            await output.WriteLineAsync($"Added board {name} ({board.Id}). Devices pair into it with the code you just typed.");
            return 0;
        }

        await output.WriteLineAsync($"Added board {name} ({board.Id}).");
        await output.WriteLineAsync($"Pairing code: {code}");
        await output.WriteLineAsync("Printed once: only the hash is kept. `board code` sets a new one.");
        return 0;
    }

    private static async Task<int> SetCodeAsync(
        AppDbContext db, string name, string code, TextWriter output, CancellationToken ct)
    {
        name = name.Trim();
        if (CodeProblem(code) is { } problem)
        {
            await output.WriteLineAsync(problem);
            return 1;
        }

        var board = await db.Boards.FirstOrDefaultAsync(b => b.Name == name, ct);
        if (board is null)
        {
            await output.WriteLineAsync($"No board called {name}; `board list` shows them.");
            return 1;
        }

        board.CodeHash = PairingCode.Hash(code);
        await db.SaveChangesAsync(ct);

        // Paired devices keep their tokens: the code is for joining, not for staying.
        await output.WriteLineAsync($"Board {name} has a new code. Devices already paired stay paired.");
        return 0;
    }

    private static async Task<int> ListAsync(AppDbContext db, TextWriter output, CancellationToken ct)
    {
        var boards = await db.Boards.OrderBy(b => b.Id).ToListAsync(ct);
        if (boards.Count == 0)
        {
            await output.WriteLineAsync("No boards. `board invite <name>` makes one.");
            return 0;
        }

        foreach (var board in boards)
        {
            var devices = await db.Devices.CountAsync(d => d.BoardId == board.Id, ct);
            var dreams = await db.Dreams.CountAsync(d => d.BoardId == board.Id, ct);
            // What the board weighs on the disk, beside the code: the operator's
            // side of the ceiling the settings screen shows (D64).
            var photographs = db.DreamImages.Where(i => db.Dreams.Any(d => d.Id == i.DreamId && d.BoardId == board.Id));
            var count = await photographs.CountAsync(ct);
            var megabytes = await photographs.SumAsync(i => i.Bytes, ct) / 1024.0 / 1024.0;
            var pairs = board.CodeHash.Length > 0 ? "pairs" : "no code";
            await output.WriteLineAsync(
                $"{board.Name}  {board.Id}  {devices} device(s), {dreams} dream(s), {count} photograph(s), {megabytes:0} MB, {pairs}");
        }

        return 0;
    }
}
