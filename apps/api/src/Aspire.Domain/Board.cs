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

    public required string Id { get; set; }

    /// <summary>The operator's name for it; the app never shows it.</summary>
    public required string Name { get; set; }

    /// <summary>
    /// The pairing code at rest (<c>PairingCode</c>), or empty for a board
    /// that cannot pair yet.
    /// </summary>
    public string CodeHash { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
