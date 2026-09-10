using System.Security.Cryptography;
using System.Text;

namespace Aspire.Api.Auth;

/// <summary>
/// A board's pairing code, at rest.
///
/// Prosper keeps its one code in configuration, where a dump of the database
/// cannot reach it. Aspire keeps one per board, in the boards table, so it is
/// hashed — and not with SHA-256: twelve digits is 10^12 guesses, which a
/// graphics card gets through in an afternoon against a plain hash. PBKDF2 at
/// a hundred thousand rounds makes the same dump a matter of years, and costs
/// a pairing attempt a few milliseconds it never notices.
///
/// Stored as one self-describing string, so the rounds can be raised later
/// without a column: <c>pbkdf2-sha256$rounds$salt$hash</c>, salt and hash in
/// base64.
/// </summary>
public static class PairingCode
{
    private const string Scheme = "pbkdf2-sha256";
    private const int Rounds = 100_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public static string Hash(string code)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(code, salt, Rounds);
        return $"{Scheme}${Rounds}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Constant-time on the hash. A malformed or empty stored value never
    /// matches and never throws: a board with no code simply cannot pair.
    /// </summary>
    public static bool Matches(string stored, string given)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != Scheme) return false;
        if (!int.TryParse(parts[1], out var rounds) || rounds < 1) return false;

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Derive(given, salt, rounds);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] Derive(string code, byte[] salt, int rounds) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(code), salt, rounds, HashAlgorithmName.SHA256, HashBytes);
}
