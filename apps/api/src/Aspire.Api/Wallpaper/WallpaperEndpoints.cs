using Aspire.Api.Auth;
using Aspire.Api.Dreams;
using Aspire.Api.Images;
using Aspire.Domain;
using Aspire.Infrastructure.Media;

namespace Aspire.Api.Wallpaper;

/// <summary>
/// The lock-screen collage, under <c>/api/v1/wallpaper</c> (PLAN.md §3.5).
///
/// One request, one image, nothing kept: the collage is made from files that
/// are already on disk and streamed straight back, so there is no second
/// tree to own, to prune, or to get the ownership of wrong (D26). The phone
/// saves it to its own photo library and sets it from there.
/// </summary>
public static class WallpaperEndpoints
{
    public static void MapWallpaper(this WebApplication app)
    {
        app.MapGet("/api/v1/wallpaper", async (
            string? dreams,
            int? width,
            int? height,
            HttpContext http,
            DeviceAuth auth,
            DreamService service,
            ImageService images,
            MediaStore media,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var asked = ParseIds(dreams);
            if (asked is null || asked.Count == 0)
            {
                return Results.Problem("Vyber aspoň jeden sen.", statusCode: 400);
            }

            if (asked.Count > CollageLayout.MaxPhotographs)
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

            // The board's own dreams, and only those: the ids came from the
            // request, so a dream on another board is simply not among them.
            var mine = (await service.ListAsync(device.BoardId, ct)).Select(d => d.Id).ToHashSet();
            var chosen = asked.Where(mine.Contains).ToList();

            var byDream = (await images.OfDreamsAsync(chosen, ct))
                .Where(image => image.ProcessedAt is not null && image.Kind == DreamImageKind.Dreamt)
                .ToLookup(image => image.DreamId);

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
                    files.Add(new CollageRenderer.Photograph(path, image.FocusX, image.FocusY, image.Zoom));
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
        });
    }

    /// <summary>
    /// The comma-separated ids of the query, or null when any of them is not
    /// an id at all. Duplicates are kept: the same dream twice on a wallpaper
    /// is the person's business.
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
