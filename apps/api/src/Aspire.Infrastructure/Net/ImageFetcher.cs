using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using Aspire.Infrastructure.Media;
using SixLabors.ImageSharp;

namespace Aspire.Infrastructure.Net;

/// <summary>What came back from a link, or the sentence for why nothing did.</summary>
public sealed record FetchedImage(Stream? Jpeg, string? Problem)
{
    public static FetchedImage No(string problem) => new(null, problem);
}

/// <summary>
/// A photograph from a link somebody pasted (D56).
///
/// The phone cannot do this itself — an image on another origin is not
/// readable by script, and a Pinterest page is not readable at all — so the
/// server fetches it. Which means the server makes requests on somebody's
/// behalf, and every rule in here exists because of that: the scheme, the
/// port, the address check on every hop (<see cref="PrivateAddress"/>), the
/// redirect ceiling, the size ceiling, the timeout, and the content type.
///
/// Two kinds of link work. A direct image is taken as it is. An HTML page is
/// read for its <c>og:image</c> — which is how a Pinterest pin says which
/// picture it is about — and that is fetched instead. The page itself is
/// never kept, never shown and never parsed beyond a regular expression over
/// its head: rule 3 reaches the API too, and ImageSharp is the one media
/// dependency it has.
/// </summary>
public sealed partial class ImageFetcher(HttpClient http)
{
    /// <summary>The most a picture may weigh, which is the upload's own cap.</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>How much of a page to read looking for the tag. It is in the head.</summary>
    public const int MaxHtmlBytes = 512 * 1024;

    /// <summary>
    /// How many hops to follow. `pin.it` is one, and Pinterest's country
    /// redirect can be a second; past three somebody is playing a game.
    /// </summary>
    public const int MaxHops = 3;

    /// <summary>The longest edge sent back, which is what the client sends up.</summary>
    public const int LongestEdge = 2048;

    private const string Problem = "Z tohohle odkazu fotku nedostanu. Ulož si ji do telefonu a vyber ji.";

    /// <summary>
    /// The picture behind a link, as a JPEG the client can treat exactly like
    /// a file somebody picked.
    ///
    /// One sentence for everything that can go wrong, and deliberately so: a
    /// message that said *which* address was refused, or what a host answered,
    /// would make this endpoint a way to ask the internet questions from
    /// inside the VPS and read the answers.
    /// </summary>
    public async Task<FetchedImage> FetchAsync(string? link, CancellationToken ct = default)
    {
        if (!IsFetchable(link, out var url)) return FetchedImage.No(Problem);

        var (response, error) = await GetAsync(url, ct);
        if (response is null) return FetchedImage.No(error ?? Problem);

        using (response)
        {
            var type = response.Content.Headers.ContentType?.MediaType ?? string.Empty;

            if (type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return await ReadImageAsync(response, ct);
            }

            if (!type.Contains("html", StringComparison.OrdinalIgnoreCase))
            {
                return FetchedImage.No(Problem);
            }

            var page = await ReadTextAsync(response, ct);
            if (page is null) return FetchedImage.No(Problem);

            var named = PictureOf(page, response.RequestMessage?.RequestUri ?? url);
            if (named is null) return FetchedImage.No(Problem);

            // One more hop, through the same fence, and this one must be a
            // picture: a page that names another page is not a photograph.
            foreach (var candidate in Candidates(named))
            {
                var (second, _) = await GetAsync(candidate, ct);
                if (second is null) continue;

                using (second)
                {
                    var secondType = second.Content.Headers.ContentType?.MediaType ?? string.Empty;
                    if (!secondType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) continue;

                    var picture = await ReadImageAsync(second, ct);
                    if (picture.Jpeg is not null) return picture;
                }
            }

            return FetchedImage.No(Problem);
        }
    }

    /// <summary>
    /// A link worth trying: http or https, a host, and a port the web is on.
    /// Anything else — a file, a gopher, a port that is somebody's database —
    /// is refused before a socket is opened.
    /// </summary>
    public static bool IsFetchable(string? link, out Uri url)
    {
        url = null!;
        if (string.IsNullOrWhiteSpace(link)) return false;
        if (link.Length > 2048) return false;
        if (!Uri.TryCreate(link.Trim(), UriKind.Absolute, out var parsed)) return false;

        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) return false;
        if (parsed.Port is not (80 or 443)) return false;
        if (string.IsNullOrWhiteSpace(parsed.Host)) return false;

        url = parsed;
        return true;
    }

    /// <summary>
    /// The bigger version of a picture, where the host is one whose sizes are
    /// in the path. Pinterest serves the same photograph at several widths and
    /// names them in the URL, and <c>og:image</c> often points at a small one;
    /// a 736 px picture on a phone screen is visibly soft, and everything here
    /// is scaled down to 2048 anyway. Strictly best-effort: the original link
    /// is always tried after it.
    /// </summary>
    public static IEnumerable<Uri> Candidates(Uri named)
    {
        if (named.Host.EndsWith("pinimg.com", StringComparison.OrdinalIgnoreCase))
        {
            var bigger = PinSize().Replace(named.AbsoluteUri, "/originals/", 1);
            if (bigger != named.AbsoluteUri && Uri.TryCreate(bigger, UriKind.Absolute, out var big))
            {
                yield return big;
            }
        }

        yield return named;
    }

    /// <summary>
    /// The picture an HTML page says it is about: Open Graph first, then
    /// Twitter's, which is what the rest of the web fell back to. A regular
    /// expression rather than a parser — this reads one tag out of a head,
    /// never renders anything, and keeps nothing.
    /// </summary>
    public static Uri? PictureOf(string html, Uri page)
    {
        foreach (var match in MetaTag().Matches(html).Cast<Match>())
        {
            var tag = match.Value;
            if (!Names(tag, "og:image") && !Names(tag, "twitter:image")) continue;

            var content = Content().Match(tag);
            if (!content.Success) continue;

            var value = System.Net.WebUtility.HtmlDecode(content.Groups[1].Value).Trim();
            if (value.Length == 0) continue;

            // Relative against the page it was found on, which is also how a
            // browser would read it.
            if (Uri.TryCreate(page, value, out var found) && IsFetchable(found.AbsoluteUri, out var ok))
            {
                return ok;
            }
        }

        return null;
    }

    private static bool Names(string tag, string property) =>
        tag.Contains($"\"{property}\"", StringComparison.OrdinalIgnoreCase) ||
        tag.Contains($"'{property}'", StringComparison.OrdinalIgnoreCase) ||
        tag.Contains($"={property}", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// One request, with the redirects walked by hand so every hop's address
    /// is checked rather than only the first — a host that answers 302 to
    /// <c>http://169.254.169.254/</c> is the whole trick.
    /// </summary>
    private async Task<(HttpResponseMessage? Response, string? Problem)> GetAsync(
        Uri url, CancellationToken ct)
    {
        var here = url;

        for (var hop = 0; hop <= MaxHops; hop++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, here);
            // A browser's shape, because a picture host that decides it does
            // not serve robots serves nothing useful at all.
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/124.0 Safari/537.36");
            request.Headers.Accept.ParseAdd("image/avif,image/webp,image/*,text/html;q=0.9,*/*;q=0.8");
            request.Headers.AcceptLanguage.ParseAdd("cs,en;q=0.8");

            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException
                                      && !ct.IsCancellationRequested)
            {
                // Refused by the address check, not answering, or too slow.
                // Which of the three is not this endpoint's news to break.
                return (null, Problem);
            }

            if (!IsRedirect(response.StatusCode) || response.Headers.Location is null)
            {
                if (!response.IsSuccessStatusCode)
                {
                    response.Dispose();
                    return (null, Problem);
                }

                if (response.Content.Headers.ContentLength > MaxBytes)
                {
                    response.Dispose();
                    return (null, Problem);
                }

                return (response, null);
            }

            var next = response.Headers.Location;
            response.Dispose();

            var absolute = next.IsAbsoluteUri ? next : new Uri(here, next);
            if (!IsFetchable(absolute.AbsoluteUri, out var checkedNext)) return (null, Problem);
            here = checkedNext;
        }

        return (null, Problem);
    }

    private static bool IsRedirect(System.Net.HttpStatusCode status) =>
        (int)status is 301 or 302 or 303 or 307 or 308;

    /// <summary>The body, up to the cap, turned into a JPEG of at most 2048 px.</summary>
    private static async Task<FetchedImage> ReadImageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var bytes = await ReadAsync(response, MaxBytes, ct);
        if (bytes is null) return FetchedImage.No(Problem);

        using var source = new MemoryStream(bytes);
        // The same gate an upload goes through: a content type is a claim, and
        // ImageSharp reading the header is the fact.
        if (!await ImageProcessor.IsImageAsync(source, ct)) return FetchedImage.No(Problem);

        var jpeg = new MemoryStream();
        try
        {
            await ImageProcessor.DownscaleAsync(source, jpeg, LongestEdge, ct);
        }
        catch (Exception e) when (e is UnknownImageFormatException or InvalidImageContentException
                                  or NotSupportedException)
        {
            await jpeg.DisposeAsync();
            return FetchedImage.No(Problem);
        }

        jpeg.Position = 0;
        return new FetchedImage(jpeg, null);
    }

    /// <summary>
    /// The head of a page, and only the head.
    ///
    /// Truncated at the cap rather than refused at it, which a picture is not:
    /// a news page is megabytes of script and the tag being looked for is in
    /// the first few kilobytes, so giving up on a big page would mean giving
    /// up on most of the web.
    /// </summary>
    private static async Task<string?> ReadTextAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var bytes = await ReadAsync(response, MaxHtmlBytes, ct, truncate: true);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// The body, up to the cap. The header's own length is a claim; this
    /// counts. Past the cap a picture is refused — it is too big to be one of
    /// ours — and a page is cut short, because what is wanted is in its head.
    /// </summary>
    private static async Task<byte[]?> ReadAsync(
        HttpResponseMessage response, long cap, CancellationToken ct, bool truncate = false)
    {
        try
        {
            await using var body = await response.Content.ReadAsStreamAsync(ct);
            using var kept = new MemoryStream();
            var buffer = new byte[81920];

            while (kept.Length < cap)
            {
                var read = await body.ReadAsync(buffer, ct);
                if (read == 0) break;

                if (kept.Length + read > cap)
                {
                    if (!truncate) return null;
                    kept.Write(buffer, 0, (int)(cap - kept.Length));
                    break;
                }

                kept.Write(buffer, 0, read);
            }

            return kept.ToArray();
        }
        catch (Exception e) when (e is HttpRequestException or IOException or TaskCanceledException
                                  && !ct.IsCancellationRequested)
        {
            return null;
        }
    }

    [GeneratedRegex("<meta\\s[^>]*>", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
    private static partial Regex MetaTag();

    [GeneratedRegex("content\\s*=\\s*[\"']([^\"']+)[\"']", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
    private static partial Regex Content();

    [GeneratedRegex("/\\d+x\\d*/", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
    private static partial Regex PinSize();
}
