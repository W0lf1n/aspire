using Aspire.Api.Auth;
using Aspire.Api.Boards;
using Aspire.Api.Dreams;
using Aspire.Api.Images;
using Aspire.Domain;
using Aspire.Infrastructure.Media;

namespace Aspire.Api.Wallpaper;

/// <summary>
/// The lock-screen collage, under <c>/api/v1/wallpaper</c> (PLAN.md §3.5) and
/// — for a morning automation that has no token — under
/// <c>/api/v1/w/{key}</c> (D60).
///
/// One request, one image, nothing kept: the collage is made from files that
/// are already on disk and streamed straight back, so there is no second
/// tree to own, to prune, or to get the ownership of wrong (D26). The phone
/// saves it to its own photo library and sets it from there.
///
/// Asked for without any dreams named, it answers with today's six —
/// <see cref="WallpaperPick"/>, the same rule the Tapeta screen starts from —
/// and stamps nothing while it does. A lock screen is not the board opening,
/// and „shown“ is a word about the board's own turn (D25, D59).
/// </summary>
public static class WallpaperEndpoints
{
    public static void MapWallpaper(this WebApplication app, string linkPolicy)
    {
        app.MapGet("/api/v1/wallpaper", async (
            string? dreams,
            int? width,
            int? height,
            int? offset,
            HttpContext http,
            DeviceAuth auth,
            DreamService service,
            ImageService images,
            MediaStore media,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            return await CollageAsync(device.BoardId, dreams, width, height, offset, service, images, media, ct);
        });

        // The same collage with no header on the request, because the thing
        // asking is a Shortcut on a locked phone (D60). The key in the path is
        // the whole permission, and it opens this and nothing else.
        app.MapGet("/api/v1/w/{key}", async (
            string key,
            int? width,
            int? height,
            int? offset,
            BoardLinks links,
            DreamService service,
            ImageService images,
            MediaStore media,
            HttpContext http,
            CancellationToken ct) =>
        {
            var boardId = await links.BoardOfAsync(key, ct);
            // A key that opens no board is a key that was revoked, or never
            // was one. Either way this is a URL that is not there — and it
            // says nothing else, because a different answer for „revoked“ and
            // „never existed“ is a question anybody could ask.
            if (boardId is null) return Results.NotFound();

            // Today's collage, made fresh on every fetch. Nothing between here
            // and the phone should keep yesterday's.
            http.Response.Headers.CacheControl = "no-store";

            return await CollageAsync(boardId, null, width, height, offset, service, images, media, ct);
        }).RequireRateLimiting(linkPolicy);
    }

    /// <summary>
    /// One collage for one board: the dreams asked for, or today's six when
    /// none were. The board comes from the caller — a token on one road, a key
    /// on the other — and never from the request's own body.
    /// </summary>
    private static async Task<IResult> CollageAsync(
        string boardId,
        string? asked,
        int? width,
        int? height,
        int? offset,
        DreamService service,
        ImageService images,
        MediaStore media,
        CancellationToken ct)
    {
        var ids = ParseIds(asked);
        if (ids is null) return Results.Problem("Tenhle odkaz na tapetu neumím.", statusCode: 400);

        if (ids.Count > CollageLayout.MaxPhotographs)
        {
            return Results.Problem(
                $"Na tapetu se vejde nejvýš {CollageLayout.MaxPhotographs} snů.",
                statusCode: 400);
        }

        var canvas = (Width: width ?? CollageRenderer.DefaultWidth,
                      Height: height ?? CollageRenderer.DefaultHeight);
        if (!CollageRenderer.IsCanvas(canvas.Width, canvas.Height))
        {
            return Results.Problem("Tenhle rozměr tapety neumím.", statusCode: 400);
        }

        // The device's own day, as the pick reads it. A link bakes the phone's
        // offset in, so the collage belongs to the morning the phone is having.
        var minutes = offset ?? 0;
        if (!NudgeSchedule.IsUtcOffset(minutes))
        {
            return Results.Problem("Tenhle časový posun neexistuje.", statusCode: 400);
        }

        var board = await service.ListAsync(boardId, ct);

        // Every ready dreamt photograph on the board, in one read: the choice
        // by hand needs the ones it named, and today's six needs to know which
        // dreams have a picture at all before it can rank them.
        var byDream = (await images.OfDreamsAsync(board.Select(d => d.Id).ToList(), ct))
            .Where(image => image.ProcessedAt is not null && image.Kind == DreamImageKind.Dreamt)
            .ToLookup(image => image.DreamId);

        List<Guid> chosen;
        if (ids.Count > 0)
        {
            // The board's own dreams, and only those: the ids came from the
            // request, so a dream on another board is simply not among them.
            var mine = board.Select(d => d.Id).ToHashSet();
            chosen = ids.Where(mine.Contains).ToList();
        }
        else
        {
            var utcNow = DateTimeOffset.UtcNow;
            var photographed = byDream.Select(group => group.Key).ToHashSet();
            chosen = WallpaperPick
                .From(
                    board,
                    photographed,
                    DailyPick.From(board, utcNow, minutes)?.Id,
                    utcNow,
                    minutes,
                    CollageLayout.MaxPhotographs)
                .Select(d => d.Id)
                .ToList();
        }

        // In the order they were asked for, which is the order the person
        // chose them in; a dream whose photograph is not ready drops out.
        // Each one carries its own crop, so the cell shows what the reel
        // shows rather than whatever is in the middle (D54).
        var files = new List<CollageRenderer.Photograph>();
        foreach (var id in chosen)
        {
            var image = byDream[id].FirstOrDefault();
            if (image is null) continue;

            var path = media.PathOf(id, image.Id, "full");
            if (File.Exists(path))
            {
                files.Add(new CollageRenderer.Photograph(path, image.FocusX, image.FocusY, image.CropZoom));
            }
        }

        if (files.Count == 0)
        {
            return Results.Problem("Vybrané sny zatím nemají fotku.", statusCode: 400);
        }

        var jpeg = new MemoryStream();
        await CollageRenderer.RenderAsync(files, canvas.Width, canvas.Height, jpeg, ct);
        jpeg.Position = 0;

        return Results.File(jpeg, "image/jpeg", "aspire-tapeta.jpg");
    }

    /// <summary>
    /// The comma-separated ids of the query, or null when any of them is not
    /// an id at all. Duplicates are kept: the same dream twice on a wallpaper
    /// is the person's business. Nothing at all means today's six.
    /// </summary>
    private static List<Guid>? ParseIds(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];

        var ids = new List<Guid>();
        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Guid.TryParse(part, out var id)) return null;
            ids.Add(id);
        }

        return ids;
    }
}
