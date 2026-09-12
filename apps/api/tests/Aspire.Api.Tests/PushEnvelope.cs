using System.Security.Cryptography;
using System.Text;
using Aspire.Infrastructure.Push;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The browser's half of a push message: RFC 8188 §2 read backwards, with
/// RFC 8291 §3.4's key derivation from the receiver's point of view.
///
/// Only a test needs it — the server never decrypts anything — but two do.
/// <see cref="WebPushCryptoTests"/> uses it to make the round trip the fixed
/// vector cannot make on its own, and <see cref="NudgeWorkerTests"/> uses it
/// to read the Czech a phone would actually have shown, which is the only way
/// to check the morning from the outside.
/// </summary>
internal static class PushEnvelope
{
    /// <summary>RFC 8291 §5's receiver, whose private half is published.</summary>
    public const string ReceiverPublic =
        "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";

    public const string ReceiverPrivate = "q1dXpw3UpT5VOmu_cf_v6ih07Aems3njxI-JWgLcM94";
    public const string AuthSecret = "BTBZMqHH6r4Tts7J_aSIgg";

    /// <summary>What the receiver reads out of the body sent to it.</summary>
    public static string Open(
        byte[] body,
        string uaPublic = ReceiverPublic,
        string uaPrivate = ReceiverPrivate,
        string authSecret = AuthSecret)
    {
        var salt = body[..16];
        var idlen = body[20];
        var asPublic = body[21..(21 + idlen)];
        var payload = body[(21 + idlen)..];

        using var receiver = ECDiffieHellman.Create(WebPushCrypto.KeyPair(uaPublic, uaPrivate));
        using var sender = ECDiffieHellman.Create(new ECParameters
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = asPublic[1..33], Y = asPublic[33..65] }
        });

        var ecdhSecret = receiver.DeriveRawSecretAgreement(sender.PublicKey);

        var keyInfo = Concat(
            Encoding.ASCII.GetBytes("WebPush: info\0"),
            WebPushCrypto.FromBase64Url(uaPublic),
            asPublic,
            [0x01]);
        var ikm = HMACSHA256.HashData(WebPushCrypto.FromBase64Url(authSecret), ecdhSecret);
        ikm = HMACSHA256.HashData(ikm, keyInfo);

        var prk = HMACSHA256.HashData(salt, ikm);
        var cek = HMACSHA256.HashData(prk, Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0\x01"))[..16];
        var nonce = HMACSHA256.HashData(prk, Encoding.ASCII.GetBytes("Content-Encoding: nonce\0\x01"))[..12];

        var ciphertext = payload[..^16];
        var tag = payload[^16..];
        var record = new byte[ciphertext.Length];
        using var aes = new AesGcm(cek, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, record);

        // The last byte is RFC 8188's delimiter, not the message.
        Assert.Equal(0x02, record[^1]);
        return Encoding.UTF8.GetString(record[..^1]);
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var all = new byte[parts.Sum(p => p.Length)];
        var at = 0;
        foreach (var part in parts)
        {
            part.CopyTo(all, at);
            at += part.Length;
        }

        return all;
    }
}
