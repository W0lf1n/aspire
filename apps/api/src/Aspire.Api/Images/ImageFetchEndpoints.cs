using Aspire.Api.Auth;
using Aspire.Infrastructure.Net;

namespace Aspire.Api.Images;

/// <summary>
/// A photograph from a link, under <c>/api/v1/images/fetch</c> (D56).
///
/// It answers with the picture itself rather than putting it on a dream: the
/// add screen has no dream yet when the link is pasted, and coming back to
/// the phone means the picture goes through exactly the path a picked file
/// does — the preview, the crop editor, the same upload. One endpoint, no
/// second write path, and nothing on the server to undo if the person
/// changes their mind.
/// </summary>
public static class ImageFetchEndpoints
{
    public static void MapImageFetch(this WebApplication app, string ratePolicy)
    {
        app.MapGet("/api/v1/images/fetch", async (
            string? url,
            HttpContext http,
            DeviceAuth auth,
            ImageFetcher fetcher,
            CancellationToken ct) =>
        {
            // Paired devices only. This makes the server fetch a URL, so it is
            // not something the internet gets to ask for.
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var got = await fetcher.FetchAsync(url, ct);
            if (got.Jpeg is null) return Results.Problem(got.Problem, statusCode: 400);

            // Nothing is kept: the bytes go to the phone and the server
            // forgets them, as the wallpaper does (D33).
            return Results.File(got.Jpeg, "image/jpeg");
        }).RequireRateLimiting(ratePolicy);
    }
}
