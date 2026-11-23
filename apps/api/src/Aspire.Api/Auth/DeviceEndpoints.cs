using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Api.Auth;

/// <summary>
/// The board's devices, and taking one off it (D93). A token never expires, so
/// deleting its row is the only way a lost phone, or a person who should
/// leave, stops reading the board; the device's push subscriptions go with it,
/// by the foreign key.
/// </summary>
public static class DeviceEndpoints
{
    public static void MapDevices(this WebApplication app)
    {
        var paired = app.MapPaired();

        // Oldest first, so the list does not reorder itself as devices are used.
        paired.MapGet("/api/v1/devices", async (HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var me = http.Device();
            var devices = await db.Devices.AsNoTracking()
                .Where(d => d.BoardId == me.BoardId)
                .ToListAsync(ct);

            return Results.Ok(devices
                .OrderBy(d => d.PairedAt)
                .ThenBy(d => d.Id, StringComparer.Ordinal)
                .Select(d => new DeviceDto(d.Id, d.Name, d.PairedAt, d.LastSeenAt, d.Id == me.Id))
                .ToList());
        });

        // Odpojit on the device itself: its row goes before it forgets its token.
        paired.MapDelete("/api/v1/device", async (HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var me = http.Device();
            await db.Devices.Where(d => d.Id == me.Id).ExecuteDeleteAsync(ct);
            return Results.NoContent();
        });

        // Any device of this board, the last one included: the board stays,
        // and the pairing code opens it again. Another board's device is a
        // 404, exactly as one that does not exist.
        paired.MapDelete("/api/v1/devices/{id}", async (
            string id, HttpContext http, AppDbContext db, CancellationToken ct) =>
        {
            var me = http.Device();
            var removed = await db.Devices
                .Where(d => d.Id == id && d.BoardId == me.BoardId)
                .ExecuteDeleteAsync(ct);
            return removed == 0 ? Results.NotFound() : Results.NoContent();
        });
    }
}
