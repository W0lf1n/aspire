using System.Net;
using Aspire.Infrastructure.Push;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The headers one nudge travels under, and what the sender makes of the
/// answer.
///
/// The encryption has the specification's own numbers to check itself against
/// (<see cref="WebPushCryptoTests"/>); what this covers is the envelope, which
/// is where the one delivery bug so far lived. A nudge set for 07:00 was sent
/// at 07:00:02 and arrived at 08:17, because every message went out marked
/// <c>Urgency: normal</c> — a priority a push service is allowed to sit on
/// until the phone is convenient to reach (D51). Nothing about that was
/// visible in a test, so now it is.
/// </summary>
public sealed class WebPushSenderTests
{
    // RFC 8291 §5's keys: real P-256 material, so the body actually encrypts.
    private const string ReceiverPublic =
        "BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4";
    private const string AuthSecret = "BTBZMqHH6r4Tts7J_aSIgg";
    private const string VapidPublic =
        "BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8";
    private const string VapidPrivate = "yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw";

    private const string Endpoint = "https://push.example/device/abc";

    [Fact]
    public async Task A_nudge_asks_to_be_delivered_now_rather_than_when_convenient()
    {
        var handler = new Recorder(HttpStatusCode.Created);

        var result = await Send(handler);

        Assert.Equal(PushResult.Sent, result);
        // The whole of D51: `normal` here is an hour in a queue.
        Assert.Equal("high", handler.Header("Urgency"));
    }

    [Fact]
    public async Task The_envelope_says_what_the_body_is_and_how_long_to_hold_it()
    {
        var handler = new Recorder(HttpStatusCode.Created);

        await Send(handler);

        Assert.Equal("application/octet-stream", handler.ContentType);
        Assert.Equal("aes128gcm", handler.ContentEncoding);
        Assert.Equal(
            WebPushSender.TimeToLiveSeconds.ToString(),
            handler.Header("TTL"));
        // RFC 8292: the signature and the public key, under one scheme.
        Assert.StartsWith("vapid t=", handler.Header("Authorization"));
        Assert.Contains($"k={VapidPublic}", handler.Header("Authorization"));
        Assert.NotEmpty(handler.Body);
    }

    [Theory]
    [InlineData(HttpStatusCode.Created, PushResult.Sent)]
    [InlineData(HttpStatusCode.OK, PushResult.Sent)]
    // The browser dropped the subscription, or the app was uninstalled: the
    // row should go rather than be tried again every morning forever.
    [InlineData(HttpStatusCode.NotFound, PushResult.Gone)]
    [InlineData(HttpStatusCode.Gone, PushResult.Gone)]
    // Not the subscription's fault, so it stays and tomorrow tries again.
    [InlineData(HttpStatusCode.InternalServerError, PushResult.Failed)]
    [InlineData(HttpStatusCode.TooManyRequests, PushResult.Failed)]
    public async Task What_the_push_service_said_decides_whether_the_row_survives(
        HttpStatusCode answered, PushResult expected)
    {
        Assert.Equal(expected, await Send(new Recorder(answered)));
    }

    [Fact]
    public async Task Keys_that_are_not_keys_are_gone_and_never_leave_the_house()
    {
        var handler = new Recorder(HttpStatusCode.Created);

        var result = await new WebPushSender(new HttpClient(handler)).SendAsync(
            Endpoint, "not-a-key", AuthSecret, "{}", VapidPublic, VapidPrivate, "mailto:a@b.c");

        Assert.Equal(PushResult.Gone, result);
        Assert.False(handler.Asked, "A subscription that cannot be encrypted for is not worth a request.");
    }

    [Fact]
    public async Task A_push_service_that_does_not_answer_is_tried_again_tomorrow()
    {
        var result = await new WebPushSender(new HttpClient(new Dead())).SendAsync(
            Endpoint, ReceiverPublic, AuthSecret, "{}", VapidPublic, VapidPrivate, "mailto:a@b.c");

        Assert.Equal(PushResult.Failed, result);
    }

    private static Task<PushResult> Send(Recorder handler) =>
        new WebPushSender(new HttpClient(handler)).SendAsync(
            Endpoint,
            ReceiverPublic,
            AuthSecret,
            "{\"title\":\"Dnešní sen\"}",
            VapidPublic,
            VapidPrivate,
            "mailto:petr@example.com");

    /// <summary>One canned answer, and the request that earned it.</summary>
    private sealed class Recorder(HttpStatusCode answer) : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _headers = [];

        public bool Asked { get; private set; }
        public string? ContentType { get; private set; }
        public string? ContentEncoding { get; private set; }
        public byte[] Body { get; private set; } = [];

        public string Header(string name) => _headers.TryGetValue(name, out var value) ? value : string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            Asked = true;
            // Read here: the sender disposes the request the moment it is done.
            foreach (var (name, values) in request.Headers) _headers[name] = string.Join(",", values);
            ContentType = request.Content?.Headers.ContentType?.ToString();
            ContentEncoding = request.Content is null
                ? null
                : string.Join(",", request.Content.Headers.ContentEncoding);
            Body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(ct);

            return new HttpResponseMessage(answer);
        }
    }

    /// <summary>A push service that is not there.</summary>
    private sealed class Dead : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("No route to host.");
    }
}
