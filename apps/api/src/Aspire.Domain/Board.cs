namespace Aspire.Domain;

/// <summary>
/// One board: the tenant. Prosper has exactly one ledger per server; Aspire
/// has as many boards as there are pairing codes, each with its own dreams
/// and its own devices, and nothing that crosses between them. There is
/// still no account and no login — a device pairs into the board whose code
/// it typed, and that is the whole of membership (D6, D21).
/// </summary>
public sealed class Board
{
    public const int NameMaxLength = 120;
    public const int CodeHashMaxLength = 128;

    /// <summary>Mirrors <c>ShareKey.MaxLength</c>: 43 characters, with room.</summary>
    public const int LinkKeyMaxLength = 64;

    public required string Id { get; set; }

    /// <summary>The operator's name for it; the app never shows it.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// The pairing code at rest (<c>PairingCode</c>), or empty for a board
    /// that cannot pair yet.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>
    /// The key in the link the morning automation fetches the lock screen
    /// with, or null when this board has never made one (D60).
    ///
    /// Not hashed, unlike <see cref="CodeHash"/>, and the difference is the
    /// point: a pairing code is twelve digits a person types, so a dump of the
    /// table would be worth guessing against. This is 32 bytes from the
    /// operating system's own randomness, which nothing guesses — and it has
    /// to be looked up by, not compared against, because the request arrives
    /// with no board attached to it. What it opens is one collage; one tap
    /// makes a new one and the old link stops working.
    /// </summary>
    public string? LinkKey { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
