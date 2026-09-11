using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Aspire.Infrastructure.Push;

/// <summary>What the push service said about one message.</summary>
public enum PushResult
{
    /// <summary>Taken. It is the phone's now.</summary>
    Sent,

    /// <summary>
    /// The service says this subscription no longer exists — the browser
    /// dropped it, or the app was uninstalled. The row should go: keeping it
    /// means trying again every morning forever.
    /// </summary>
    Gone,

    /// <summary>Anything else: worth a line in the log and another try tomorrow.</summary>
    Failed
}

/// <summary>
/// One encrypted message to one push service (RFC 8030): the body from
/// <see cref="WebPushCrypto"/>, a VAPID authorization, and the two headers
/// the service needs to know what to do with it.
/// </summary>
public sealed class WebPushSender(HttpClient http)
{
    /// <summary>
    /// How long a push service should hold a message for a phone that is
    /// off. A morning nudge is worth having an hour later and not worth
    /// having the next day, and the schedule already refuses to send one
    /// late (<c>NudgeSchedule.GraceMinutes</c>).
    /// </summary>
    public const int TimeToLiveSeconds = 4 * 60 * 60;

    /// <summary>
    /// How long the VAPID claim is good for. Twelve hours is well inside the
    /// 24 RFC 8292 allows, and long enough that one is signed per send
    /// without any clock skew mattering.
    /// </summary>
    private static readonly TimeSpan TokenLife = TimeSpan.FromHours(12);

    public async Task<PushResult> SendAsync(
        string endpoint,
        string p256dh,
        string auth,
        string message,
        string vapidPublicKey,
        string vapidPrivateKey,
        string subject,
        CancellationToken ct = default)
    {
        byte[] body;
        try
        {
            body = WebPushCrypto.Encrypt(
                Encoding.UTF8.GetBytes(message),
                WebPushCrypto.FromBase64Url(p256dh),
                WebPushCrypto.FromBase64Url(auth));
        }
        catch (Exception e) when (e is FormatException or ArgumentException)
        {
            // Keys that are not keys: this subscription can never be sent to.
            return PushResult.Gone;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new ByteArrayContent(body);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        request.Content.Headers.ContentEncoding.Add("aes128gcm");
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            WebPushCrypto.VapidAuthorization(
                WebPushCrypto.AudienceOf(endpoint),
                subject,
                vapidPublicKey,
                vapidPrivateKey,
                DateTimeOffset.UtcNow.Add(TokenLife)));
        request.Headers.TryAddWithoutValidation("TTL", TimeToLiveSeconds.ToString());
        // The phone is asleep; waking it is the whole point.
        request.Headers.TryAddWithoutValidation("Urgency", "normal");

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return PushResult.Sent;

            return response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone
                ? PushResult.Gone
                : PushResult.Failed;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            // The push service is not answering. Not the subscription's fault,
            // so it stays and is tried again tomorrow.
            return PushResult.Failed;
        }
    }
}
