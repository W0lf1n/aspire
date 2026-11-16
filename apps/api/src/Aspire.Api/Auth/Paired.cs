using Aspire.Domain;

namespace Aspire.Api.Auth;

/// <summary>
/// The routes a paired device calls. The bearer token is resolved once, in the
/// group's filter, so an endpoint cannot be mapped there and forget to ask;
/// the handler reads the device back with <see cref="Device(HttpContext)"/>.
/// The routes that answer with no token — pairing, and the two keys of rule 17
/// — are mapped on the app itself, outside this group.
/// </summary>
public static class Paired
{
    public static RouteGroupBuilder MapPaired(this IEndpointRouteBuilder app) =>
        app.MapGroup("").AddEndpointFilter(async (context, next) =>
        {
            var http = context.HttpContext;
            var auth = http.RequestServices.GetRequiredService<DeviceAuth>();
            var device = await auth.ResolveAsync(http.Request.Headers.Authorization, http.RequestAborted);
            if (device is null) return Results.Unauthorized();

            http.Items[typeof(Device)] = device;
            return await next(context);
        });

    /// <summary>The device the group's filter resolved. Only inside <see cref="MapPaired"/>.</summary>
    public static Device Device(this HttpContext http) => (Device)http.Items[typeof(Device)]!;
}
