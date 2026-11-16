using Aspire.Api.Auth;

namespace Aspire.Api.Boards;

/// <summary>
/// The board's lock-screen link, under <c>/api/v1/board/link</c> (D60).
///
/// Three verbs and no more: read what there is, make one — which replaces
/// whatever was there, so making is also revoking — and delete it. A paired
/// device only: the link is a capability, so handing one out is a thing a
/// device on the board does, never something the internet can ask for.
///
/// What comes back is the path rather than the whole URL. The app is served
/// from the same origin as this API and always has been, so the browser knows
/// the origin for certain, while the server behind nginx would be guessing at
/// its own scheme from a header a client can set.
/// </summary>
public static class LinkEndpoints
{
    public static void MapBoardLink(this WebApplication app)
    {
        var paired = app.MapPaired();

        paired.MapGet("/api/v1/board/link", async (
            HttpContext http,
            BoardLinks links,
            CancellationToken ct) =>
        {
            var device = http.Device();

            var key = await links.KeyOfAsync(device.BoardId, ct);
            return Results.Ok(new LinkResponse(key is null ? null : BoardLinks.PathOf(key)));
        });

        paired.MapPost("/api/v1/board/link", async (
            HttpContext http,
            BoardLinks links,
            CancellationToken ct) =>
        {
            var device = http.Device();

            // A board has one link. Asking again makes a new key and the old
            // one stops opening anything, which is what revoking looks like
            // when there is nothing to revoke but a random number.
            var key = await links.MakeAsync(device.BoardId, ct);
            return Results.Ok(new LinkResponse(BoardLinks.PathOf(key)));
        });

        paired.MapDelete("/api/v1/board/link", async (
            HttpContext http,
            BoardLinks links,
            CancellationToken ct) =>
        {
            var device = http.Device();

            // Deleting a link that is not there is not a failure: the board
            // ends up with no link either way, which is what was asked.
            await links.RevokeAsync(device.BoardId, ct);
            return Results.NoContent();
        });
    }
}
