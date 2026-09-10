using Aspire.Api.Auth;
using Aspire.Api.Images;

namespace Aspire.Api.Dreams;

/// <summary>
/// The dreams and their photographs, under <c>/api/v1/dreams</c>.
///
/// Every handler resolves the device first and hands its board to the
/// service, which never sees a board it was not handed. A dream on another
/// board is a 404, not a 403: that board's existence is not this device's
/// business either.
/// </summary>
public static class DreamEndpoints
{
    public static void MapDreams(this WebApplication app)
    {
        app.MapGet("/api/v1/dreams", async (
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var rows = await dreams.ListAsync(device.BoardId, ct);
            var byDream = (await images.OfDreamsAsync(rows.Select(d => d.Id).ToList(), ct))
                .ToLookup(i => i.DreamId);
            return Results.Ok(rows.Select(d => DreamDto.From(d, byDream[d.Id])).ToList());
        });

        app.MapGet("/api/v1/dreams/{id:guid}", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var dream = await dreams.FindAsync(device.BoardId, id, ct);
            if (dream is null) return Results.NotFound();

            return Results.Ok(DreamDto.From(dream, await images.OfDreamsAsync([dream.Id], ct)));
        });

        app.MapPost("/api/v1/dreams", async (
            DreamInput input,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (DreamService.Problem(input) is { } problem) return Results.Problem(problem, statusCode: 400);

            var dream = await dreams.CreateAsync(device.BoardId, input, ct);
            return Results.Created($"/api/v1/dreams/{dream.Id}", DreamDto.From(dream, []));
        });

        app.MapPut("/api/v1/dreams/{id:guid}", async (
            Guid id,
            DreamInput input,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (DreamService.Problem(input) is { } problem) return Results.Problem(problem, statusCode: 400);

            var dream = await dreams.UpdateAsync(device.BoardId, id, input, ct);
            if (dream is null) return Results.NotFound();

            return Results.Ok(DreamDto.From(dream, await images.OfDreamsAsync([dream.Id], ct)));
        });

        app.MapDelete("/api/v1/dreams/{id:guid}", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            return await dreams.DeleteAsync(device.BoardId, id, ct) ? Results.NoContent() : Results.NotFound();
        });

        app.MapPost("/api/v1/dreams/{id:guid}/likes", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var dream = await dreams.LikeAsync(device.BoardId, id, ct);
            if (dream is null) return Results.NotFound();

            return Results.Ok(DreamDto.From(dream, await images.OfDreamsAsync([dream.Id], ct)));
        });

        // ── the photographs ───────────────────────────────────────────────────

        app.MapPost("/api/v1/dreams/{id:guid}/images", async (
            Guid id,
            IFormFile file,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var dream = await dreams.FindAsync(device.BoardId, id, ct);
            if (dream is null) return Results.NotFound();

            await using var upload = file.OpenReadStream();
            var (image, problem) = await images.AddAsync(dream, upload, file.Length, ct);
            if (image is null) return Results.Problem(problem, statusCode: 400);

            // 202: the row is there, the sizes follow. `Ready` says when.
            return Results.Accepted(null, DreamImageDto.From(image));
        }).DisableAntiforgery(); // A bearer token, not a cookie: there is nothing to forge.

        app.MapDelete("/api/v1/dreams/{id:guid}/images/{imageId:guid}", async (
            Guid id,
            Guid imageId,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var dream = await dreams.FindAsync(device.BoardId, id, ct);
            if (dream is null) return Results.NotFound();

            return await images.RemoveAsync(dream, imageId, ct) ? Results.NoContent() : Results.NotFound();
        });
    }
}
