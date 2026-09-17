using Aspire.Api.Auth;
using Aspire.Api.Images;
using Aspire.Domain;
using Aspire.Infrastructure.Media;

namespace Aspire.Api.Dreams;

/// <summary>
/// One dream, shared with somebody who has nothing (PLAN.md §3.7, D61).
///
/// Two roads, as the lock screen has: the board's own devices make and revoke
/// the key under <c>/api/v1/dreams/{id}/link</c>, and anybody at all reads the
/// page at <c>/s/{key}</c>. The second one answers with no token, so it is
/// written the way D60's is — the key is the whole permission, it opens
/// exactly one dream, a key that opens nothing gets a bare 404, and making a
/// new one is how the old link dies.
/// </summary>
public static class DreamLinkEndpoints
{
    /// <summary>The card a chat app shows: the OG rectangle, near enough everywhere.</summary>
    private const int CardWidth = 1200;
    private const int CardHeight = 630;

    public static void MapDreamLinks(this WebApplication app, string sharePolicy)
    {
        app.MapGet("/api/v1/dreams/{id:guid}/link", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamLinks links,
            DreamService dreams,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (await dreams.FindAsync(device.BoardId, id, ct) is null) return Results.NotFound();

            var key = await links.KeyOfAsync(device.BoardId, id, ct);
            return Results.Ok(new LinkResponse(key is null ? null : DreamLinks.PathOf(key)));
        });

        app.MapPost("/api/v1/dreams/{id:guid}/link", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamLinks links,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            // A dream has one link. Asking again makes a new key and the old
            // one stops opening anything, which is what revoking looks like
            // when there is nothing to revoke but a random number.
            var key = await links.MakeAsync(device.BoardId, id, ct);
            return key is null
                ? Results.NotFound()
                : Results.Ok(new LinkResponse(DreamLinks.PathOf(key)));
        });

        app.MapDelete("/api/v1/dreams/{id:guid}/link", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamLinks links,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            // Revoking a link that is not there is not a failure: the dream
            // ends up unshared either way, which is what was asked.
            await links.RevokeAsync(device.BoardId, id, ct);
            return Results.NoContent();
        });

        // ── what the other person opens ─────────────────────────────────────

        app.MapGet("/s/{key}", async (
            string key,
            HttpContext http,
            DreamLinks links,
            ImageService images,
            CancellationToken ct) =>
        {
            var dream = await links.FindAsync(key, ct);
            // „Revoked“ and „never existed“ must not be two different answers.
            if (dream is null) return Results.NotFound();

            var photo = await PhotographAsync(dream, images, ct);

            // Not stored anywhere in between: a page that showed a title the
            // person has since changed, or a dream they have since unshared,
            // would be the link outliving the decision to share it.
            http.Response.Headers.CacheControl = "no-store";

            return Results.Content(
                SharePage.Render(dream, photo, OriginOf(http)),
                "text/html; charset=utf-8");
        }).RequireRateLimiting(sharePolicy);

        // The preview picture, as a JPEG at the shape a chat app draws. The
        // photographs on disk are WebP, which some of those apps still will
        // not render in a preview card — and a card that silently fails is
        // the whole reason this is a page and not a route of the app.
        app.MapGet("/s/{key}/card.jpg", async (
            string key,
            HttpContext http,
            DreamLinks links,
            ImageService images,
            MediaStore media,
            CancellationToken ct) =>
        {
            var dream = await links.FindAsync(key, ct);
            if (dream is null) return Results.NotFound();

            var photo = await PhotographAsync(dream, images, ct);
            if (photo is null) return Results.NotFound();

            var path = media.PathOf(dream.Id, photo.Id, "full");
            if (!File.Exists(path)) return Results.NotFound();

            // One photograph through the collage renderer, which already knows
            // how to fill a rectangle with a picture cropped where the person
            // put it (D54).
            var jpeg = new MemoryStream();
            await CollageRenderer.RenderAsync(
                [new CollageRenderer.Photograph(path, photo.FocusX, photo.FocusY, photo.CropZoom)],
                CardWidth,
                CardHeight,
                jpeg,
                ct);
            jpeg.Position = 0;

            http.Response.Headers.CacheControl = "no-store";
            return Results.File(jpeg, "image/jpeg");
        }).RequireRateLimiting(sharePolicy);
    }

    /// <summary>
    /// Which photograph a shared dream shows: the dreamt one, which is the
    /// board's picture of it, and the achieved one only when that is all there
    /// is (D28). The page is a tile, and a tile shows the dream.
    /// </summary>
    private static async Task<DreamImage?> PhotographAsync(
        Dream dream, ImageService images, CancellationToken ct)
    {
        var ready = (await images.OfDreamsAsync([dream.Id], ct))
            .Where(image => image.ProcessedAt is not null)
            .ToList();

        return ready.FirstOrDefault(image => image.Kind == DreamImageKind.Dreamt)
               ?? ready.FirstOrDefault();
    }

    /// <summary>
    /// Where this server is, as the person's browser reached it. Open Graph
    /// wants absolute URLs, so unlike every other link in this app the server
    /// has to name itself: the scheme from the edge's own
    /// <c>X-Forwarded-Proto</c> (the host vhost sets it from <c>$scheme</c>),
    /// and the host from the request. The worst a wrong guess costs is a
    /// preview card that does not draw.
    /// </summary>
    private static string OriginOf(HttpContext http)
    {
        var forwarded = http.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        var scheme = forwarded is "http" or "https" ? forwarded : http.Request.Scheme;
        return $"{scheme}://{http.Request.Host}";
    }
}
