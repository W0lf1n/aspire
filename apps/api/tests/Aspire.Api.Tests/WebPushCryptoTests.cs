using System.Security.Cryptography;
using System.Text;
using Aspire.Infrastructure.Push;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The specification's own numbers. RFC 8291 §5 publishes a worked push
/// message encryption with every key, the salt and the resulting body, so
/// this code is checked against the standard rather than against itself
/// (D35). If any label, any ordering or any length here is wrong, the body
/// comes out different and this fails.
/// </summary>
public sealed class WebPushCryptoTests
{
    // ── RFC 8291 §5, verbatim ───────────────────────────────────────────────

    private const string Plaintext = "When I grow up, I want to be a watermelon";
    private const string AuthSecret = "BTBZMqHH6r4Tts7J_aSIgg";
    private const string ReceiverPrivate = "q1dXpw3UpT5VOmu_cf_v6ih07Aems3njxI-JWgLcM94";
    private const string ReceiverPublic =
        "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";
    private const string SenderPrivate = "yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw";
    private const string SenderPublic =
        "BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8";
    private const string Salt = "DGv6ra1nlYgDCS1FRnbzlw";

    private const string ExpectedBody =
        "DGv6ra1nlYgDCS1FRnbzlwAAEABBBP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27ml" +
        "mlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A_yl95bQpu6cVPT" +
        "pK4Mqgkf1CXztLVBSt2Ks3oZwbuwXPXLWyouBWLVWGNWQexSgSxsj_Qulcy4a-fN";

    [Fact]
    public void The_example_from_the_specification_comes_out_byte_for_byte()
    {
        var body = WebPushCrypto.Encrypt(
            Encoding.UTF8.GetBytes(Plaintext),
            WebPushCrypto.FromBase64Url(ReceiverPublic),
            WebPushCrypto.FromBase64Url(AuthSecret),
            WebPushCrypto.KeyPair(SenderPublic, SenderPrivate),
            WebPushCrypto.FromBase64Url(Salt));

        Assert.Equal(ExpectedBody, WebPushCrypto.ToBase64Url(body));
    }

    [Fact]
    public void The_receiver_can_read_what_was_written_for_it()
    {
        // The other half of the example: decrypted with the user agent's own
        // private key, following RFC 8188's header back out. A real browser
        // does exactly this, so a round trip is the interoperability check
        // the fixed vector cannot make on its own.
        var body = WebPushCrypto.Encrypt(
            Encoding.UTF8.GetBytes(Plaintext),
            WebPushCrypto.FromBase64Url(ReceiverPublic),
            WebPushCrypto.FromBase64Url(AuthSecret));

        Assert.Equal(Plaintext, Decrypt(body, ReceiverPublic, ReceiverPrivate, AuthSecret));
    }

    [Fact]
    public void A_fresh_message_is_never_the_same_twice()
    {
        var ua = WebPushCrypto.FromBase64Url(ReceiverPublic);
        var auth = WebPushCrypto.FromBase64Url(AuthSecret);
        var message = Encoding.UTF8.GetBytes(Plaintext);

        // A new sender key pair and salt every time (RFC 8291 §3.1), so two
        // sends of one sentence never produce the same bytes.
        Assert.NotEqual(
            WebPushCrypto.ToBase64Url(WebPushCrypto.Encrypt(message, ua, auth)),
            WebPushCrypto.ToBase64Url(WebPushCrypto.Encrypt(message, ua, auth)));
    }

    [Fact]
    public void A_key_that_is_not_a_p256_point_is_refused_before_anything_is_derived()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WebPushCrypto.Encrypt(
            "x"u8,
            new byte[10],
            WebPushCrypto.FromBase64Url(AuthSecret)));
    }

    // ── the authorization header (RFC 8292) ─────────────────────────────────

    [Fact]
    public void The_vapid_header_is_a_jwt_the_push_service_can_check()
    {
        var (publicKey, privateKey) = WebPushCrypto.NewVapidKeys();
        var expires = DateTimeOffset.UtcNow.AddHours(12);

        var header = WebPushCrypto.VapidAuthorization(
            "https://push.example.net", "mailto:petr@example.com", publicKey, privateKey, expires);

        Assert.StartsWith("vapid t=", header);
        var token = header["vapid t=".Length..header.IndexOf(", k=", StringComparison.Ordinal)];
        Assert.Equal($"k={publicKey}", header[(header.IndexOf(", k=", StringComparison.Ordinal) + 2)..]);

        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);

        // The claims say who and until when, and the signature is over the
        // first two parts with the public half of the pair we handed out.
        var claims = Encoding.UTF8.GetString(WebPushCrypto.FromBase64Url(parts[1]));
        Assert.Contains("\"aud\":\"https://push.example.net\"", claims);
        Assert.Contains("\"sub\":\"mailto:petr@example.com\"", claims);
        Assert.Contains($"\"exp\":{expires.ToUnixTimeSeconds()}", claims);

        using var verifier = ECDsa.Create(WebPushCrypto.KeyPair(publicKey, privateKey));
        Assert.True(verifier.VerifyData(
            Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}"),
            WebPushCrypto.FromBase64Url(parts[2]),
            HashAlgorithmName.SHA256,
            DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
    }

    [Theory]
    [InlineData("https://push.example.net/push/JzLQ3raZJfFBR0aqvOMsLrt54w4rJUsV", "https://push.example.net")]
    [InlineData("https://fcm.googleapis.com/fcm/send/abc123", "https://fcm.googleapis.com")]
    public void The_audience_is_the_origin_and_never_the_subscription(string endpoint, string audience)
    {
        Assert.Equal(audience, WebPushCrypto.AudienceOf(endpoint));
    }

    [Fact]
    public void A_new_pair_is_a_p256_point_and_a_scalar()
    {
        var (publicKey, privateKey) = WebPushCrypto.NewVapidKeys();

        var point = WebPushCrypto.FromBase64Url(publicKey);
        Assert.Equal(WebPushCrypto.PublicKeyLength, point.Length);
        Assert.Equal(0x04, point[0]);
        Assert.Equal(32, WebPushCrypto.FromBase64Url(privateKey).Length);
        // base64url: no padding and none of base64's two awkward characters.
        Assert.DoesNotContain('=', publicKey);
        Assert.DoesNotContain('+', publicKey);
        Assert.DoesNotContain('/', publicKey);
    }

    // ── the user agent's side, for the round trip ───────────────────────────

    /// <summary>
    /// RFC 8188 §2 read backwards, with RFC 8291 §3.4's key derivation from
    /// the receiver's point of view. Only the test needs it: the server never
    /// decrypts anything.
    /// </summary>
    private static string Decrypt(byte[] body, string uaPublic, string uaPrivate, string authSecret)
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
