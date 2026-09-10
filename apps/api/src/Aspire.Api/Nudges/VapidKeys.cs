namespace Aspire.Api.Nudges;

/// <summary>
/// The server's VAPID key pair, from configuration (PLAN.md §4).
///
/// Configuration rather than the database, and generated once by hand rather
/// than at start: the public key is baked into every browser subscription
/// this server has ever handed out, so a pair that regenerated itself would
/// silently orphan every one of them. It belongs with the pairing code, in
/// the environment, where a value that must not change lives (D34).
///
/// A server with no pair configured does not do notifications, says so, and
/// runs perfectly well otherwise — which is what a laptop wants.
/// </summary>
public sealed class VapidKeys
{
    public VapidKeys(string? publicKey, string? privateKey, string? subject)
    {
        PublicKey = publicKey?.Trim() ?? string.Empty;
        PrivateKey = privateKey?.Trim() ?? string.Empty;
        Subject = string.IsNullOrWhiteSpace(subject) ? "mailto:aspire@localhost" : subject.Trim();
    }

    /// <summary>Base64url, uncompressed P-256 point; the browser is given this.</summary>
    public string PublicKey { get; }

    /// <summary>Base64url, the P-256 private scalar. Never leaves the server.</summary>
    public string PrivateKey { get; }

    /// <summary>The `mailto:` or `https:` the push service can complain to.</summary>
    public string Subject { get; }

    public bool Configured => PublicKey.Length > 0 && PrivateKey.Length > 0;
}
