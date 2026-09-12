using Aspire.Api.Auth;
using Aspire.Api.Images;
using Aspire.Domain;

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

        // The board opened with this dream today. Nothing comes back: the
        // stamp is for tomorrow's pick and for the board's other devices.
        app.MapPost("/api/v1/dreams/{id:guid}/shown", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            return await dreams.MarkShownAsync(device.BoardId, id, ct) ? Results.NoContent() : Results.NotFound();
        });

        // ── Teď, the second reel (D53) ────────────────────────────────────────

        // Put this dream on Teď, behind the ones already there. 409 when it
        // is full, because the cap is the feature: a person who is focusing
        // on eleven things is not focusing.
        app.MapPost("/api/v1/dreams/{id:guid}/focus", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var (dream, problem) = await dreams.AddToFocusAsync(device.BoardId, id, ct);
            if (problem is not null) return Results.Problem(problem, statusCode: StatusCodes.Status409Conflict);
            if (dream is null) return Results.NotFound();

            return Results.Ok(DreamDto.From(dream, await images.OfDreamsAsync([dream.Id], ct)));
        });

        app.MapDelete("/api/v1/dreams/{id:guid}/focus", async (
            Guid id,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            return await dreams.RemoveFromFocusAsync(device.BoardId, id, ct)
                ? Results.NoContent()
                : Results.NotFound();
        });

        // The whole of Teď at once: these dreams, in this order. One request
        // rather than a move per arrow, so the ten are never half-ordered.
        app.MapPut("/api/v1/focus", async (
            FocusInput input,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var (rows, problem) = await dreams.ReorderFocusAsync(
                device.BoardId, input.DreamIds ?? [], ct);
            if (problem is not null) return Results.Problem(problem, statusCode: 400);

            var ordered = rows ?? [];
            var byDream = (await images.OfDreamsAsync(ordered.Select(d => d.Id).ToList(), ct))
                .ToLookup(i => i.DreamId);
            return Results.Ok(ordered.Select(d => DreamDto.From(d, byDream[d.Id])).ToList());
        });

        // ── the photographs ───────────────────────────────────────────────────

        // `kind` says which of the two photographs this is: the dreamt one
        // by default, the achieved one when the dream came true (D28). A
        // dream's status is not checked — a status can change after, and the
        // answer to that must never be deleting somebody's photograph.
        // `focusX`, `focusY` and `zoom` ride in the query, because a
        // photograph is positioned before it is sent: on the add screen there
        // is no dream yet to hang a second request on (D54).
        app.MapPost("/api/v1/dreams/{id:guid}/images", async (
            Guid id,
            IFormFile file,
            string? kind,
            double? focusX,
            double? focusY,
            double? zoom,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (DreamImageKindNames.TryParse(kind) is not { } asked)
            {
                return Results.Problem("Fotka je buď vysněná, nebo skutečná.", statusCode: 400);
            }

            var dream = await dreams.FindAsync(device.BoardId, id, ct);
            if (dream is null) return Results.NotFound();

            await using var upload = file.OpenReadStream();
            var (image, problem) = await images.AddAsync(
                dream, upload, file.Length, asked, new FocalInput(focusX, focusY, zoom), ct);
            // A board at its ceiling is a 409, as a full Teď is: not a bad
            // request, a board that has to lose something first (D64).
            if (image is null)
            {
                var status = problem == ImageService.BoardFull ? StatusCodes.Status409Conflict : 400;
                return Results.Problem(problem, statusCode: status);
            }

            // 202: the row is there, the sizes follow. `Ready` says when.
            return Results.Accepted(null, DreamImageDto.From(image));
        }).DisableAntiforgery(); // A bearer token, not a cookie: there is nothing to forge.

        // Where an existing photograph is looked at (D54). The files on disk
        // are untouched — the crop is metadata, so this is instant and costs
        // no resize.
        app.MapPut("/api/v1/dreams/{id:guid}/images/{imageId:guid}", async (
            Guid id,
            Guid imageId,
            FocalInput input,
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

            var (image, problem) = await images.MoveAsync(dream, imageId, input, ct);
            if (problem is not null) return Results.Problem(problem, statusCode: 400);
            if (image is null) return Results.NotFound();

            return Results.Ok(DreamImageDto.From(image));
        });

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
