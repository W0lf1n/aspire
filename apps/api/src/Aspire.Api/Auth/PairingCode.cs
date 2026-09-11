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

    /// <summary>
    /// A fresh code, <paramref name="digits"/> of them, from the operating
    /// system's own randomness — never <c>Random</c>, which is seeded from the
    /// clock and would make two boards created in the same second guessable
    /// from each other.
    ///
    /// The leading digit is never zero, as <c>DEPLOYMENT.md</c>'s own
    /// <c>shuf</c> line has always produced: a code is read aloud, written
    /// down and typed back, and a leading zero is the digit that gets lost on
    /// the way. It costs a sixth of a bit out of forty.
    ///
    /// <c>GetInt32</c> rather than a byte and a remainder: a remainder over a
    /// range that does not divide 256 makes the low digits likelier, which is
    /// exactly the bias a guesser starts from.
    /// </summary>
    public static string Generate(int digits)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(digits, 2);

        var code = new char[digits];
        code[0] = (char)('1' + RandomNumberGenerator.GetInt32(0, 9));
        for (var i = 1; i < digits; i++) code[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        return new string(code);
    }

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
