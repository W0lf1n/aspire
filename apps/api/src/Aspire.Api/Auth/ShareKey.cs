using System.Buffers.Text;
using System.Security.Cryptography;

namespace Aspire.Api.Auth;

/// <summary>
/// A key that is its own permission: the lock-screen link's (D60) and the
/// share link's (D61) are the same thirty-two bytes, so they are made in one
/// place and mean one thing.
///
/// Not a token and not a code. A pairing code is twelve digits a person types,
/// so it is hashed and compared in constant time (<see cref="PairingCode"/>);
/// this is 256 bits nobody types and nobody guesses, and it arrives with no
/// account on it at all — so it is looked *up* by, and the fence around the
/// endpoint is there for the work the request causes, never for the key.
///
/// What a key opens is always exactly one thing, and making a new one is how
/// the old one is revoked.
/// </summary>
public static class ShareKey
{
    public const int Bytes = 32;

    /// <summary>Thirty-two bytes in base64url is 43 characters; 64 leaves room.</summary>
    public const int MaxLength = 64;

    public static string New() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(Bytes));

    /// <summary>
    /// Whether this is worth asking the database about. Not a check that it is
    /// a real key — only the table knows that — but a string that could not be
    /// one should not become a query.
    /// </summary>
    public static bool CouldBe(string? key) =>
        !string.IsNullOrEmpty(key) && key.Length <= MaxLength;
}
