using Aspire.Api.Auth;
using Aspire.Domain;

namespace Aspire.Api.Nudges;

/// <summary>
/// The morning nudge, under <c>/api/v1/nudge</c> (PLAN.md §3.6).
///
/// A device tells the server its browser's subscription and when it wants to
/// hear from it; the worker does the rest. There is no screen on the server
/// and no list: a device can read and set its own, and nothing else.
/// </summary>
public static class NudgeEndpoints
{
    public static void MapNudges(this WebApplication app)
    {
        // The key a browser needs before it can subscribe at all. No auth: it
        // is a public key, and a client that has not paired yet still has to
        // be able to find out whether this server does notifications.
        app.MapGet("/api/v1/nudge/key", (VapidKeys keys) =>
            Results.Ok(new PushKeyResponse(keys.PublicKey)));

        // What this device's nudge is set to. The endpoint identifies the
        // device, so it comes in the query rather than being guessed.
        app.MapGet("/api/v1/nudge", async (
            string? endpoint,
            HttpContext http,
            DeviceAuth auth,
            NudgeService nudges,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(endpoint)) return Results.Ok(NudgeDto.None);

            var found = await nudges.FindAsync(device.BoardId, endpoint.Trim(), ct);
            return Results.Ok(found is null ? NudgeDto.None : NudgeDto.From(found));
        });

        app.MapPut("/api/v1/nudge", async (
            NudgeInput input,
            HttpContext http,
            DeviceAuth auth,
            NudgeService nudges,
            VapidKeys keys,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            // A server with no key pair cannot send anything, and a
            // subscription it kept would be a promise it cannot keep.
            if (!keys.Configured) return Results.Problem("Server neumí posílat upozornění.", statusCode: 503);

            if (NudgeService.Problem(input) is { } problem) return Results.Problem(problem, statusCode: 400);

            // Off is not a subscription; it is the absence of one.
            if (input.Mode == NudgeMode.Off)
            {
                await nudges.RemoveAsync(device.BoardId, input.Endpoint!.Trim(), ct);
                return Results.Ok(NudgeDto.None);
            }

            return Results.Ok(NudgeDto.From(await nudges.SaveAsync(device.BoardId, input, ct)));
        });

        // Where the device is, every time the app opens (D34's promise, kept
        // by D51). It moves the offset on a subscription that exists and
        // makes none: a device that has not asked to be nudged gets the same
        // 204 as one that has, because whether this phone wants a morning
        // dream is not something a request like this should be able to learn.
        app.MapPost("/api/v1/nudge/offset", async (
            NudgeOffsetInput input,
            HttpContext http,
            DeviceAuth auth,
            NudgeService nudges,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (NudgeService.Problem(input) is { } problem) return Results.Problem(problem, statusCode: 400);

            await nudges.UpdateOffsetAsync(
                device.BoardId, input.Endpoint!.Trim(), input.UtcOffsetMinutes!.Value, ct);
            return Results.NoContent();
        });

        app.MapDelete("/api/v1/nudge", async (
            string? endpoint,
            HttpContext http,
            DeviceAuth auth,
            NudgeService nudges,
            CancellationToken ct) =>
        {
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
            if (device is null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(endpoint)) return Results.NoContent();

            await nudges.RemoveAsync(device.BoardId, endpoint.Trim(), ct);
            return Results.NoContent();
        });
    }
}
