using Aspire.Api.Auth;
using Aspire.Api.Images;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Boards;

/// <summary>
/// The board as a whole, under <c>/api/v1/board</c>: its name, how many
/// photographs it holds on the server and what they weigh, against the
/// ceiling (D64). Read by Nastavení → Stahování, so the person sees the
/// number long before an upload is refused with a sentence.
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
    }
}
