using Aspire.Api.Auth;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Boards;

/// <summary>
/// <c>board add | code | list</c>, run in the API's own image so it has the
/// database and the same hashing. There is no signup form and there is not
/// going to be one (D6): a board is something the operator makes.
///
/// <code>
/// docker compose exec api dotnet Aspire.Api.dll board add Zuzana 483920174635
/// docker compose exec api dotnet Aspire.Api.dll board code Nástěnka 209384756123
/// docker compose exec api dotnet Aspire.Api.dll board list
/// </code>
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

    private const string Usage =
        "usage: board add <name> <code> | board code <name> <code> | board list";

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

    private static async Task<int> AddAsync(
        AppDbContext db, string name, string code, TextWriter output, CancellationToken ct)
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

        await output.WriteLineAsync($"Added board {name} ({board.Id}). Devices pair into it with the code you just typed.");
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
            await output.WriteLineAsync("No boards. `board add <name> <code>` makes one.");
            return 0;
        }

        foreach (var board in boards)
        {
            var devices = await db.Devices.CountAsync(d => d.BoardId == board.Id, ct);
            var dreams = await db.Dreams.CountAsync(d => d.BoardId == board.Id, ct);
            var pairs = board.CodeHash.Length > 0 ? "pairs" : "no code";
            await output.WriteLineAsync($"{board.Name}  {board.Id}  {devices} device(s), {dreams} dream(s), {pairs}");
        }

        return 0;
    }
}
