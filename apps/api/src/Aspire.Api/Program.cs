using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Aspire.Api;
using Aspire.Api.Auth;
using Aspire.Api.Boards;
using Aspire.Api.Dreams;
using Aspire.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// camelCase on the wire, because the client is TypeScript and the contracts
// package is the shared definition. Enums travel as their kebab-case names —
// `in-progress` — which is also how the database stores them.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower));
});

// Postgres in production (PLAN.md §4). SQLite exists so the server can be run
// and tested on a laptop with no Docker — it is the same EF model either way.
// Migrations are Postgres-only; the SQLite side creates its schema directly.
var provider = builder.Configuration["Database:Provider"] ?? "postgres";
var usesSqlite = string.Equals(provider, "sqlite", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usesSqlite)
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")
                          ?? "Data Source=aspire.db");
    }
    else
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"));
    }
});

builder.Services.AddScoped<DeviceAuth>();
builder.Services.AddScoped<DreamService>();

// The pairing code is the only thing between the internet and the board.
// `DEPLOYMENT.md` generates twelve digits, because the code's own length is the
// only defence here that does not depend on a fence holding. `PairingCode.Matches`
// being constant-time defends against timing, not against volume.
//
// Five attempts a minute per address turns twelve digits into a number of years
// nobody has. `ClientAddress` is what makes "per address" true. The second
// fence is `limit_req` in `deploy/nginx/app.conf`, one layer out, because a
// limiter living in this process resets every time the process restarts.
const string PairPolicy = "pair";

/// <summary>
/// Attempts an hour on the pairing endpoint as a whole, from everywhere. A
/// guess spread across many addresses is the one attack per-address counting
/// does not touch; twenty is more pairing than a household does in a year.
/// </summary>
const int PairAttemptsPerHour = 20;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
        http.Request.Path.Equals(new PathString("/api/v1/pair"), StringComparison.OrdinalIgnoreCase)
            ? RateLimitPartition.GetFixedWindowLimiter("pair-everyone", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PairAttemptsPerHour,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            })
            // Every other route is fenced by its bearer token, not by a clock.
            : RateLimitPartition.GetNoLimiter("open"));

    options.AddPolicy(PairPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        ClientAddress.PartitionKey(
            http.Request.Headers["X-Real-IP"].FirstOrDefault(),
            http.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            // No queue: a pairing attempt that has to wait is a wrong guess.
            QueueLimit = 0
        }));
});

// The client is served from the same origin in production, so there is no
// CORS policy by default and deliberately no wildcard one. An explicit
// allowlist exists only so the Vite dev server on another port can be
// developed against — and in practice Vite proxies `/api` instead.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:Origins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .WithHeaders("authorization", "content-type")
        .WithMethods("GET", "POST", "PUT", "DELETE")));
}

var app = builder.Build();

if (allowedOrigins.Length > 0) app.UseCors();

app.UseRateLimiter();

if (app.Configuration.GetValue("Database:MigrateOnStart", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Postgres walks the migration history; SQLite has none and creates the
    // schema as the model stands, which is all a laptop needs.
    if (usesSqlite) await db.Database.EnsureCreatedAsync();
    else await db.Database.MigrateAsync();

    // The configured code goes to the first board, or to one that has none.
    await BoardSeed.ApplyAsync(db, app.Configuration["Pairing:Code"]);
}

// The operator's commands run in this same image, with the database and the
// hashing the server uses, and then leave without ever listening.
if (BoardCommand.IsBoardCommand(args))
{
    // A board's name may carry a háček, and a Windows console would rather not.
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    using var scope = app.Services.CreateScope();
    return await BoardCommand.RunAsync(args, scope.ServiceProvider.GetRequiredService<AppDbContext>(), Console.Out);
}

// The media root (PLAN.md §4): a volume in production, a folder beside the
// database on a laptop. Nothing writes into it yet — M1 brings the pipeline —
// but the directory exists from the first start so the deployment is checked
// for it before there is anything to lose.
var mediaRoot = app.Configuration["Media:Root"] ?? "/data/media";
Directory.CreateDirectory(mediaRoot);

var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.1";

app.MapGet("/api/v1/health", () => Results.Ok(new HealthResponse(true, version)));

app.MapPost("/api/v1/pair", async (
    PairRequest request,
    DeviceAuth auth,
    CancellationToken ct) =>
{
    if (!await auth.AnyBoardPairsAsync(ct))
    {
        // Refusing is the safe default: a server with no code must never mean
        // "any code will do", which is how a private board becomes a public one.
        return Results.Problem("Párování není na serveru nastavené.", statusCode: 503);
    }

    // The code says which board; a code that opens none is a wrong code.
    var board = await auth.FindBoardAsync(request.Code ?? string.Empty, ct);
    if (board is null) return Results.Unauthorized();

    return Results.Ok(await auth.PairAsync(board, request.DeviceName ?? "Zařízení", ct));
}).RequireRateLimiting(PairPolicy);

// ── dreams: one board's, in board order ─────────────────────────────────────
//
// Every handler resolves the device first and hands its board to the
// service, which never sees a board it was not handed. A dream on another
// board is a 404, not a 403: that board's existence is not this device's
// business either.

app.MapGet("/api/v1/dreams", async (
    HttpContext http,
    DeviceAuth auth,
    DreamService dreams,
    CancellationToken ct) =>
{
    var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
    if (device is null) return Results.Unauthorized();

    var rows = await dreams.ListAsync(device.BoardId, ct);
    return Results.Ok(rows.Select(DreamDto.From).ToList());
});

app.MapGet("/api/v1/dreams/{id:guid}", async (
    Guid id,
    HttpContext http,
    DeviceAuth auth,
    DreamService dreams,
    CancellationToken ct) =>
{
    var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
    if (device is null) return Results.Unauthorized();

    var dream = await dreams.FindAsync(device.BoardId, id, ct);
    return dream is null ? Results.NotFound() : Results.Ok(DreamDto.From(dream));
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
    return Results.Created($"/api/v1/dreams/{dream.Id}", DreamDto.From(dream));
});

app.MapPut("/api/v1/dreams/{id:guid}", async (
    Guid id,
    DreamInput input,
    HttpContext http,
    DeviceAuth auth,
    DreamService dreams,
    CancellationToken ct) =>
{
    var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
    if (device is null) return Results.Unauthorized();

    if (DreamService.Problem(input) is { } problem) return Results.Problem(problem, statusCode: 400);

    var dream = await dreams.UpdateAsync(device.BoardId, id, input, ct);
    return dream is null ? Results.NotFound() : Results.Ok(DreamDto.From(dream));
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
    CancellationToken ct) =>
{
    var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
    if (device is null) return Results.Unauthorized();

    var dream = await dreams.LikeAsync(device.BoardId, id, ct);
    return dream is null ? Results.NotFound() : Results.Ok(DreamDto.From(dream));
});

app.Run();
return 0;

/// <summary>Named so the test host can reach it.</summary>
public partial class Program;
