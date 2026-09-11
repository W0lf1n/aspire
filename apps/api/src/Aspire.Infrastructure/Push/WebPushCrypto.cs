using System.Buffers.Binary;
using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Aspire.Infrastructure.Push;

/// <summary>
/// Encrypting one push message (RFC 8291) and signing the request that
/// carries it (RFC 8292).
///
/// Written here rather than taken from a package (D35). None of it is
/// invented: it is <c>System.Security.Cryptography</c>'s own P-256, HKDF and
/// AES-GCM composed in the order two RFCs set out, and RFC 8291 §5 publishes
/// a worked example with every value in it — so the test is the
/// specification's own numbers rather than this code agreeing with itself.
/// </summary>
public static class WebPushCrypto
{
    /// <summary>An uncompressed P-256 point: 0x04 and two 32-byte coordinates.</summary>
    public const int PublicKeyLength = 65;

    /// <summary>The user agent's authentication secret (RFC 8291 §3.2).</summary>
    public const int AuthSecretLength = 16;

    public const int SaltLength = 16;

    /// <summary>One record, as big as anything this sends will ever be.</summary>
    public const int RecordSize = 4096;

    private static readonly byte[] KeyInfoPrefix = Encoding.ASCII.GetBytes("WebPush: info\0");
    private static readonly byte[] CekInfo = Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0");
    private static readonly byte[] NonceInfo = Encoding.ASCII.GetBytes("Content-Encoding: nonce\0");

    // ── base64url, which is how every key in this protocol travels ──────────

    public static string ToBase64Url(ReadOnlySpan<byte> bytes) => Base64Url.EncodeToString(bytes);

    public static byte[] FromBase64Url(string value) => Base64Url.DecodeFromChars(value);

    // ── the message body (RFC 8291 + RFC 8188) ──────────────────────────────

    /// <summary>
    /// The <c>aes128gcm</c> body for one subscription: a fresh sender key
    /// pair and salt each time, as the RFC requires.
    /// </summary>
    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, byte[] uaPublic, byte[] authSecret)
    {
        using var sender = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        return Encrypt(plaintext, uaPublic, authSecret, sender.ExportParameters(true), salt);
    }

    /// <summary>
    /// The same, with the sender key pair and the salt given rather than
    /// made — which is what makes RFC 8291 §5 a test rather than a story.
    /// </summary>
    public static byte[] Encrypt(
        ReadOnlySpan<byte> plaintext,
        byte[] uaPublic,
        byte[] authSecret,
        ECParameters senderParameters,
        byte[] salt)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(uaPublic.Length, PublicKeyLength, nameof(uaPublic));
        ArgumentOutOfRangeException.ThrowIfNotEqual(salt.Length, SaltLength, nameof(salt));

        using var sender = ECDiffieHellman.Create(senderParameters);
        var asPublic = UncompressedPoint(senderParameters);

        using var receiver = ECDiffieHellman.Create(PublicPoint(uaPublic));
        // The raw X coordinate, which is what the RFC calls `ecdh_secret`;
        // the hashing overloads would be one KDF too many.
        var ecdhSecret = sender.DeriveRawSecretAgreement(receiver.PublicKey);

        // PRK_key = HMAC(auth_secret, ecdh_secret), then expand over
        // "WebPush: info" || 0x00 || ua_public || as_public (RFC 8291 §3.4).
        var keyInfo = new byte[KeyInfoPrefix.Length + (PublicKeyLength * 2)];
        KeyInfoPrefix.CopyTo(keyInfo, 0);
        uaPublic.CopyTo(keyInfo, KeyInfoPrefix.Length);
        asPublic.CopyTo(keyInfo, KeyInfoPrefix.Length + PublicKeyLength);

        var ikm = Expand(HmacSha256(authSecret, ecdhSecret), keyInfo, 32);

        // And then RFC 8188's own two, salted with the record's salt.
        var prk = HmacSha256(salt, ikm);
        var cek = Expand(prk, CekInfo, 16);
        var nonce = Expand(prk, NonceInfo, 12);

        // The record is the message with RFC 8188's delimiter on the end.
        // There is only ever one record here, so it is always the last: 0x02.
        var record = new byte[plaintext.Length + 1];
        plaintext.CopyTo(record);
        record[^1] = 0x02;

        var ciphertext = new byte[record.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(cek, tag.Length))
        {
            aes.Encrypt(nonce, record, ciphertext, tag);
        }

        // header = salt || record size || keyid length || keyid (RFC 8188 §2.1)
        var body = new byte[SaltLength + 4 + 1 + PublicKeyLength + ciphertext.Length + tag.Length];
        salt.CopyTo(body, 0);
        BinaryPrimitives.WriteUInt32BigEndian(body.AsSpan(SaltLength), RecordSize);
        body[SaltLength + 4] = PublicKeyLength;
        asPublic.CopyTo(body, SaltLength + 5);
        ciphertext.CopyTo(body, SaltLength + 5 + PublicKeyLength);
        tag.CopyTo(body, body.Length - tag.Length);
        return body;
    }

    // ── the Authorization header (RFC 8292) ─────────────────────────────────

    /// <summary>
    /// The <c>vapid</c> authorization for one push service: a JWT saying who
    /// this server is and how long the claim is good for, signed with the
    /// server's own key, and the public half beside it so the service can
    /// check the signature without having been told about us first.
    /// </summary>
    public static string VapidAuthorization(
        string audience,
        string subject,
        string publicKey,
        string privateKey,
        DateTimeOffset expires)
    {
        var header = ToBase64Url(Encoding.ASCII.GetBytes("{\"typ\":\"JWT\",\"alg\":\"ES256\"}"));
        var claims = ToBase64Url(Encoding.UTF8.GetBytes(
            $"{{\"aud\":\"{audience}\",\"exp\":{expires.ToUnixTimeSeconds()},\"sub\":\"{subject}\"}}"));
        var signed = Encoding.ASCII.GetBytes($"{header}.{claims}");

        using var key = ECDsa.Create(KeyPair(publicKey, privateKey));
        // IEEE P1363 is r || s, which is what JWS ES256 wants; the DER form
        // this call defaults to would be rejected by every push service.
        var signature = key.SignData(
            signed, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        return $"vapid t={header}.{claims}.{ToBase64Url(signature)}, k={publicKey}";
    }

    /// <summary>
    /// The origin a push endpoint belongs to, which is the JWT's audience:
    /// scheme and host, never the path, because the path is the subscription.
    /// </summary>
    public static string AudienceOf(string endpoint) => new Uri(endpoint).GetLeftPart(UriPartial.Authority);

    /// <summary>A fresh VAPID pair, base64url, for the operator to paste into the environment.</summary>
    public static (string PublicKey, string PrivateKey) NewVapidKeys()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(true);
        return (ToBase64Url(UncompressedPoint(parameters)), ToBase64Url(parameters.D!));
    }

    // ── the pieces ──────────────────────────────────────────────────────────

    /// <summary>P-256 parameters from the two halves as they are stored.</summary>
    public static ECParameters KeyPair(string publicKey, string privateKey)
    {
        var point = FromBase64Url(publicKey);
        ArgumentOutOfRangeException.ThrowIfNotEqual(point.Length, PublicKeyLength, nameof(publicKey));

        return new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = point[1..33], Y = point[33..65] },
            D = FromBase64Url(privateKey)
        };
    }

    private static ECParameters PublicPoint(byte[] uncompressed) => new()
    {
        Curve = ECCurve.NamedCurves.nistP256,
        Q = new ECPoint { X = uncompressed[1..33], Y = uncompressed[33..65] }
    };

    private static byte[] UncompressedPoint(ECParameters parameters)
    {
        var point = new byte[PublicKeyLength];
        point[0] = 0x04;
        parameters.Q.X!.CopyTo(point, 1);
        parameters.Q.Y!.CopyTo(point, 33);
        return point;
    }

    private static byte[] HmacSha256(byte[] key, byte[] message) => HMACSHA256.HashData(key, message);

    /// <summary>
    /// One round of HKDF-Expand, which is all any of these need: every output
    /// here is 32 octets or fewer, so the block counter never passes its
    /// first value and the whole of HKDF-Expand is one HMAC over
    /// <c>info || 0x01</c>.
    /// </summary>
    private static byte[] Expand(byte[] prk, byte[] info, int length)
    {
        var block = new byte[info.Length + 1];
        info.CopyTo(block, 0);
        block[^1] = 0x01;
        return HmacSha256(prk, block)[..length];
    }
}
