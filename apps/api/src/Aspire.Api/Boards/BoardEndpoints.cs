using Aspire.Api.Auth;
using Aspire.Api.Dreams;
using Aspire.Api.Images;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Boards;

/// <summary>
/// The board as a whole, under <c>/api/v1/board</c>: its name, how many
/// photographs it holds on the server and what they weigh, against the
/// ceiling (D64). Read by Nastavení → Stahování, so the person sees the
/// number long before an upload is refused with a sentence. And the one verb
/// a board has: starting over (D80).
/// </summary>
public static class BoardEndpoints
{
    public static void MapBoard(this WebApplication app)
    {
        app.MapGet("/api/v1/board", async (
            HttpContext http,
            DeviceAuth auth,
            AppDbContext db,
            ImageService images,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            var board = await db.Boards.AsNoTracking().FirstAsync(b => b.Id == device.BoardId, ct);
            var (photographs, bytes) = await images.UsageOfAsync(board.Id, ct);
            return Results.Ok(new BoardResponse(board.Name, photographs, bytes, ImageService.MaxBoardBytes));
        });

        // Starting over (D80): every dream and photograph on this board, and
        // nothing else. A POST with the phrase in it rather than a DELETE with
        // nothing, so emptying a board takes the sentence as well as the URL.
        app.MapPost("/api/v1/board/reset", async (
            ResetInput input,
            HttpContext http,
            DeviceAuth auth,
            DreamService dreams,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (!ResetPhrase.Matches(input.Phrase))
            {
                return Results.Problem($"Napiš „{ResetPhrase.Text}“. Nic se nesmazalo.", statusCode: 400);
            }

            return Results.Ok(new ResetResponse(await dreams.ResetAsync(device.BoardId, ct)));
        });
    }
}
