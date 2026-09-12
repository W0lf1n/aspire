using System.Net;
using System.Net.Http.Headers;
using Aspire.Infrastructure.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// A photograph from a link (D56), and every way that is refused.
///
/// The address fence itself is <see cref="PrivateAddressTests"/>; what is
/// here is the rest of the bargain — the scheme, the port, the redirect
/// ceiling, the size ceiling, the content type, and reading one tag out of a
/// page. No network: the handler is a fake, so these run in a millisecond and
/// say the same thing on a laptop with no internet.
/// </summary>
public sealed class ImageFetcherTests
{
    private const string Refused = "Z tohohle odkazu fotku nedostanu. Ulož si ji do telefonu a vyber ji.";

    private static byte[] Png(int width = 40, int height = 60)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(200, 80, 40));
        using var buffer = new MemoryStream();
        image.Save(buffer, new PngEncoder());
        return buffer.ToArray();
    }

    /// <summary>A web of canned answers, by URL, and a note of what was asked.</summary>
    private sealed class Web(Dictionary<string, HttpResponseMessage> answers) : HttpMessageHandler
    {
        public List<string> Asked { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.AbsoluteUri;
            Asked.Add(url);
            return Task.FromResult(
                answers.TryGetValue(url, out var answer)
                    ? answer
                    : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private static HttpResponseMessage Image(byte[]? bytes = null, string type = "image/png")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes ?? Png())
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(type);
        return response;
    }

    private static HttpResponseMessage Html(string body)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body)
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
        return response;
    }

    private static HttpResponseMessage Moved(string to, HttpStatusCode status = HttpStatusCode.Found)
    {
        var response = new HttpResponseMessage(status);
        response.Headers.Location = new Uri(to);
        return response;
    }

    private static (ImageFetcher Fetcher, Web Handler) Fetching(
        Dictionary<string, HttpResponseMessage> answers)
    {
        var handler = new Web(answers);
        return (new ImageFetcher(new HttpClient(handler)), handler);
    }

    // ── links that are not worth opening a socket for ───────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    // Not the web.
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/a.jpg")]
    [InlineData("gopher://example.com/")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    // A port that is somebody's database rather than somebody's website.
    [InlineData("http://example.com:5432/a.jpg")]
    [InlineData("http://example.com:6379/")]
    [InlineData("https://example.com:8080/a.jpg")]
    public void A_link_that_is_not_a_web_address_is_refused_before_anything_is_opened(string? link)
    {
        Assert.False(ImageFetcher.IsFetchable(link, out _));
    }

    [Theory]
    [InlineData("https://i.pinimg.com/736x/ab/cd.jpg", "i.pinimg.com")]
    [InlineData("http://example.com/a.jpg", "example.com")]
    // The default port written out is still the default port.
    [InlineData("https://example.com:443/a.jpg", "example.com")]
    [InlineData("  https://example.com/a.jpg  ", "example.com")]
    public void A_web_address_is_worth_trying(string link, string host)
    {
        Assert.True(ImageFetcher.IsFetchable(link, out var url));
        Assert.Equal(host, url.Host);
        Assert.Contains(url.Scheme, new[] { "http", "https" });
    }

    [Fact]
    public async Task A_refused_link_never_reaches_the_network()
    {
        var (fetcher, handler) = Fetching([]);

        var got = await fetcher.FetchAsync("file:///etc/passwd");

        Assert.Null(got.Jpeg);
        Assert.Equal(Refused, got.Problem);
        Assert.Empty(handler.Asked);
    }

    // ── a picture, directly ─────────────────────────────────────────────────

    [Fact]
    public async Task A_direct_image_comes_back_as_a_jpeg()
    {
        var (fetcher, _) = Fetching(new() { ["https://example.com/a.png"] = Image() });

        var got = await fetcher.FetchAsync("https://example.com/a.png");

        Assert.Null(got.Problem);
        Assert.NotNull(got.Jpeg);
        // Whatever came in, what goes out is what a picked file is.
        Assert.Equal("image/jpeg", SixLabors.ImageSharp.Image.DetectFormat(got.Jpeg).DefaultMimeType);
    }

    [Fact]
    public async Task A_picture_bigger_than_the_screen_comes_back_smaller()
    {
        var (fetcher, _) = Fetching(new() { ["https://example.com/big.png"] = Image(Png(4000, 3000)) });

        var got = await fetcher.FetchAsync("https://example.com/big.png");

        Assert.NotNull(got.Jpeg);
        var info = SixLabors.ImageSharp.Image.Identify(got.Jpeg);
        Assert.Equal(ImageFetcher.LongestEdge, Math.Max(info.Width, info.Height));
    }

    [Fact]
    public async Task Something_that_says_it_is_an_image_and_is_not_is_refused()
    {
        // A content type is a claim; ImageSharp reading the header is the fact.
        var (fetcher, _) = Fetching(new()
        {
            ["https://example.com/a.png"] = Image("<html>not a picture</html>"u8.ToArray())
        });

        var got = await fetcher.FetchAsync("https://example.com/a.png");

        Assert.Null(got.Jpeg);
        Assert.Equal(Refused, got.Problem);
    }

    [Fact]
    public async Task Anything_that_is_neither_a_picture_nor_a_page_is_refused()
    {
        var json = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        json.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var (fetcher, _) = Fetching(new() { ["https://example.com/a"] = json });

        Assert.Null((await fetcher.FetchAsync("https://example.com/a")).Jpeg);
    }

    [Fact]
    public async Task A_picture_past_the_cap_is_refused_rather_than_kept()
    {
        var huge = Image();
        huge.Content.Headers.ContentLength = ImageFetcher.MaxBytes + 1;
        var (fetcher, _) = Fetching(new() { ["https://example.com/huge.png"] = huge });

        Assert.Null((await fetcher.FetchAsync("https://example.com/huge.png")).Jpeg);
    }

    [Fact]
    public async Task A_host_that_will_not_answer_is_one_sentence_like_every_other_failure()
    {
        var (fetcher, _) = Fetching([]);

        var got = await fetcher.FetchAsync("https://example.com/gone.png");

        Assert.Equal(Refused, got.Problem);
    }

    // ── a page that names its picture ───────────────────────────────────────

    private const string Pin =
        """
        <html><head>
          <meta property="og:title" content="Island na kole" />
          <meta property="og:image" content="https://i.pinimg.com/736x/ab/cd/ef.jpg" />
        </head><body>…</body></html>
        """;

    [Fact]
    public async Task A_pin_page_gives_up_the_picture_it_is_about()
    {
        var (fetcher, handler) = Fetching(new()
        {
            ["https://cz.pinterest.com/pin/123/"] = Html(Pin),
            ["https://i.pinimg.com/originals/ab/cd/ef.jpg"] = Image()
        });

        var got = await fetcher.FetchAsync("https://cz.pinterest.com/pin/123/");

        Assert.Null(got.Problem);
        Assert.NotNull(got.Jpeg);
        // The bigger one is asked for first: og:image often names a small one,
        // and a 736 px picture full-bleed on a phone is visibly soft.
        Assert.Contains("https://i.pinimg.com/originals/ab/cd/ef.jpg", handler.Asked);
    }

    [Fact]
    public async Task The_named_size_is_taken_when_the_bigger_one_is_not_there()
    {
        var (fetcher, handler) = Fetching(new()
        {
            ["https://cz.pinterest.com/pin/123/"] = Html(Pin),
            ["https://i.pinimg.com/736x/ab/cd/ef.jpg"] = Image()
        });

        var got = await fetcher.FetchAsync("https://cz.pinterest.com/pin/123/");

        Assert.NotNull(got.Jpeg);
        Assert.Contains("https://i.pinimg.com/originals/ab/cd/ef.jpg", handler.Asked);
        Assert.Contains("https://i.pinimg.com/736x/ab/cd/ef.jpg", handler.Asked);
    }

    [Fact]
    public async Task Twitters_tag_is_read_when_there_is_no_open_graph_one()
    {
        var page = """<html><head><meta name="twitter:image" content="/photo.png"></head></html>""";
        var (fetcher, _) = Fetching(new()
        {
            ["https://example.com/thing"] = Html(page),
            // Relative, resolved against the page it was found on.
            ["https://example.com/photo.png"] = Image()
        });

        Assert.NotNull((await fetcher.FetchAsync("https://example.com/thing")).Jpeg);
    }

    [Fact]
    public void A_page_with_nothing_to_say_names_no_picture()
    {
        var page = new Uri("https://example.com/thing");

        Assert.Null(ImageFetcher.PictureOf("<html><head></head></html>", page));
        Assert.Null(ImageFetcher.PictureOf("""<meta property="og:title" content="x">""", page));
        // A tag naming something that is not a web address is not a picture.
        Assert.Null(ImageFetcher.PictureOf("""<meta property="og:image" content="file:///x">""", page));
    }

    [Fact]
    public void An_escaped_address_is_unescaped_before_it_is_used()
    {
        var found = ImageFetcher.PictureOf(
            """<meta property="og:image" content="https://example.com/a.png?w=1&amp;h=2">""",
            new Uri("https://example.com/"));

        Assert.Equal("https://example.com/a.png?w=1&h=2", found?.AbsoluteUri);
    }

    [Fact]
    public async Task A_page_bigger_than_the_cap_is_cut_short_rather_than_given_up_on()
    {
        // A news page is megabytes of script and the tag is in the first few
        // kilobytes. Refusing the whole page means refusing most of the web —
        // which is exactly what happened the first time this was tried
        // against a real one.
        var head = """<html><head><meta property="og:image" content="https://example.com/p.png">""";
        var page = head + new string('x', ImageFetcher.MaxHtmlBytes * 2) + "</head></html>";
        var (fetcher, _) = Fetching(new()
        {
            ["https://example.com/huge"] = Html(page),
            ["https://example.com/p.png"] = Image()
        });

        Assert.NotNull((await fetcher.FetchAsync("https://example.com/huge")).Jpeg);
    }

    [Fact]
    public async Task A_page_whose_tag_is_past_the_cap_is_simply_not_found()
    {
        var page = "<html><head>" + new string('x', ImageFetcher.MaxHtmlBytes * 2) +
                   """<meta property="og:image" content="https://example.com/p.png"></head></html>""";
        var (fetcher, _) = Fetching(new() { ["https://example.com/late"] = Html(page) });

        Assert.Null((await fetcher.FetchAsync("https://example.com/late")).Jpeg);
    }

    [Fact]
    public async Task A_page_that_names_another_page_is_not_a_photograph()
    {
        var (fetcher, _) = Fetching(new()
        {
            ["https://example.com/a"] = Html("""<meta property="og:image" content="https://example.com/b">"""),
            ["https://example.com/b"] = Html("<html></html>")
        });

        Assert.Null((await fetcher.FetchAsync("https://example.com/a")).Jpeg);
    }

    // ── redirects, which are where the fence has to hold ────────────────────

    [Fact]
    public async Task A_short_link_is_followed()
    {
        var (fetcher, handler) = Fetching(new()
        {
            ["https://pin.it/abc"] = Moved("https://cz.pinterest.com/pin/123/"),
            ["https://cz.pinterest.com/pin/123/"] = Html(Pin),
            ["https://i.pinimg.com/originals/ab/cd/ef.jpg"] = Image()
        });

        var got = await fetcher.FetchAsync("https://pin.it/abc");

        Assert.NotNull(got.Jpeg);
        Assert.Equal("https://pin.it/abc", handler.Asked[0]);
    }

    [Fact]
    public async Task A_redirect_somewhere_that_is_not_the_web_stops_there()
    {
        // The trick this exists for: a host that answers 302 to a scheme or a
        // port the first check would have refused.
        var (fetcher, handler) = Fetching(new()
        {
            ["https://example.com/a"] = Moved("http://example.com:6379/"),
            ["https://example.com/b"] = Moved("https://example.com:22/")
        });

        Assert.Null((await fetcher.FetchAsync("https://example.com/a")).Jpeg);
        Assert.Null((await fetcher.FetchAsync("https://example.com/b")).Jpeg);
        Assert.Equal(["https://example.com/a", "https://example.com/b"], handler.Asked);
    }

    [Fact]
    public async Task A_redirect_that_never_ends_stops_at_the_ceiling()
    {
        var (fetcher, handler) = Fetching(new()
        {
            ["https://example.com/loop"] = Moved("https://example.com/loop", HttpStatusCode.MovedPermanently)
        });

        Assert.Null((await fetcher.FetchAsync("https://example.com/loop")).Jpeg);
        Assert.Equal(ImageFetcher.MaxHops + 1, handler.Asked.Count);
    }

    [Fact]
    public void The_bigger_variant_is_only_offered_for_the_host_that_has_one()
    {
        Assert.Equal(
            ["https://i.pinimg.com/originals/a/b.jpg", "https://i.pinimg.com/564x/a/b.jpg"],
            ImageFetcher.Candidates(new Uri("https://i.pinimg.com/564x/a/b.jpg")).Select(u => u.AbsoluteUri));

        // Everywhere else, one candidate and no guessing at somebody's paths.
        Assert.Equal(
            ["https://example.com/564x/a/b.jpg"],
            ImageFetcher.Candidates(new Uri("https://example.com/564x/a/b.jpg")).Select(u => u.AbsoluteUri));
    }
}
