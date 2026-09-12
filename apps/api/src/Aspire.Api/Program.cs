using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Aspire.Api;
using Aspire.Api.Auth;
using Aspire.Api.Boards;
using Aspire.Api.Dreams;
using Aspire.Api.Images;
using Aspire.Api.Nudges;
using Aspire.Api.Wallpaper;
using Aspire.Infrastructure;
using Aspire.Infrastructure.Media;
using Aspire.Infrastructure.Net;
using Aspire.Infrastructure.Push;
using Microsoft.Extensions.FileProviders;
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
builder.Services.AddScoped<BoardLinks>();
builder.Services.AddScoped<DreamLinks>();
builder.Services.AddScoped<DreamService>();
builder.Services.AddScoped<NudgeService>();

// The media root (PLAN.md §4): a volume in production, a folder beside the
// database on a laptop. One store, one line, one worker.
builder.Services.AddSingleton(new MediaStore(builder.Configuration["Media:Root"] ?? "/data/media"));

// The morning nudge's key pair (PLAN.md §3.6). Absent on a laptop, and the
// server then says it does not do notifications rather than refusing to run.
builder.Services.AddSingleton(new VapidKeys(
    builder.Configuration["Push:PublicKey"],
    builder.Configuration["Push:PrivateKey"],
    builder.Configuration["Push:Subject"]));
// The morning nudge's sender and its worker (PLAN.md §3.6). The worker
// stops itself when there is no key pair to send with.
builder.Services.AddHttpClient<WebPushSender>(client =>
    client.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHostedService<NudgeWorker>();

// The link fetcher's own client, which can only reach the public internet.
// The check is in `ConnectCallback` rather than before the request, because
// a name checked and then resolved again is a name that can answer
// differently the second time — the socket is opened to an address that
// passed, and to no other (D56).
builder.Services.AddHttpClient<ImageFetcher>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        client.MaxResponseContentBufferSize = ImageFetcher.MaxBytes;
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        // Walked by hand, so every hop's address is checked (D56).
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        ConnectCallback = async (context, ct) =>
        {
            var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            var reachable = addresses.Where(PrivateAddress.IsPublic).ToArray();
            if (reachable.Length == 0)
            {
                throw new HttpRequestException("That address is not on the public internet.");
            }

            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(reachable, context.DnsEndPoint.Port, ct);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    });

builder.Services.AddSingleton<ImageQueue>();
builder.Services.AddHostedService<ImageWorker>();
builder.Services.AddScoped<ImageService>();

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
/// Fetching a picture from a link makes this server open a connection to
/// somewhere else, so it is fenced by a clock as well as by a token (D56).
/// Twenty a minute is far more than a person pasting links needs and far less
/// than anything worth borrowing the server for.
/// </summary>
const string FetchPolicy = "fetch";

/// <summary>
/// The lock-screen link renders a collage of six full-size photographs on
/// every hit, with no token in front of it — so the clock is what keeps that
/// work bounded (D60). Per address rather than per key: the cost being fenced
/// is the rendering, which an address pays for whichever key it presents, and
/// the key itself is 32 random bytes that nothing is going to guess.
///
/// Ten a minute rather than the one a fetch a morning needs, because a 429 to
/// a phone's automation is a wallpaper that silently never changes, and a
/// browser opening the link to check it often asks twice.
/// </summary>
const string LinkPolicy = "wallpaper-link";

/// <summary>
/// A shared dream's page and its preview card (D61), which also answer with no
/// token. Cheaper than the collage — one photograph, and the page itself is a
/// few kilobytes of text — and likelier to be opened by several people at
/// once, because a link in a group chat is one address as far as this counts.
/// </summary>
const string SharePolicy = "share";

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

    options.AddPolicy(FetchPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        ClientAddress.PartitionKey(
            http.Request.Headers["X-Real-IP"].FirstOrDefault(),
            http.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy(LinkPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        ClientAddress.PartitionKey(
            http.Request.Headers["X-Real-IP"].FirstOrDefault(),
            http.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy(SharePolicy, http => RateLimitPartition.GetFixedWindowLimiter(
        ClientAddress.PartitionKey(
            http.Request.Headers["X-Real-IP"].FirstOrDefault(),
            http.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
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

// `vapid` is the one command that touches nothing: it makes a key pair and
// prints it. It runs before the migration on purpose — the moment you need
// it is while setting a box up, which is before its database exists.
if (VapidCommand.IsVapidCommand(args))
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    return await VapidCommand.RunAsync(
        Console.Out, app.Services.GetRequiredService<VapidKeys>().PublicKey);
}

if (allowedOrigins.Length > 0) app.UseCors();

app.UseRateLimiter();

if (app.Configuration.GetValue("Database:MigrateOnStart", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Postgres walks the migration history; SQLite has none and creates the
    // schema as the model stands, which is all a laptop needs.
    if (usesSqlite)
    {
        await db.Database.EnsureCreatedAsync();

        // …but only the first time. `EnsureCreated` leaves an existing file
        // alone, so a model that has gained a column since leaves a file that
        // answers 500 to the first request rather than anything to the start
        // (D55). Better to be told now, with the fix in the sentence.
        if (await SqliteSchema.BehindTheModelAsync(db) is { } complaint)
        {
            app.Logger.LogError(
                "The laptop database is behind the model: {Complaint} " +
                "Delete {File} and start the API again — SQLite mode creates the schema as the " +
                "model stands and never migrates it (D5). The dreams in it go with it.",
                complaint,
                SqliteSchema.FileOf(app.Configuration.GetConnectionString("Sqlite")));
            return 1;
        }
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    // The configured code goes to the first board, or to one that has none.
    await BoardSeed.ApplyAsync(db, app.Configuration["Pairing:Code"]);
}

if (BoardCommand.IsBoardCommand(args))
{
    // A board's name may carry a háček, and a Windows console would rather not.
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    using var scope = app.Services.CreateScope();
    return await BoardCommand.RunAsync(args, scope.ServiceProvider.GetRequiredService<AppDbContext>(), Console.Out);
}

// The directory exists and can be written to from the first start, so a
// deployment that cannot keep a photograph says so here rather than one
// upload at a time. nginx serves it as `/media/` in production and the API
// never sees a byte of it; on a laptop there is no nginx, so the API serves
// the same tree with the same year of cache.
var media = app.Services.GetRequiredService<MediaStore>();
media.EnsureWritable();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(media.Root),
    RequestPath = "/media",
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable"
});

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

// The dreams and their photographs live in Dreams/DreamEndpoints.cs, and
// the lock-screen collage in Wallpaper/WallpaperEndpoints.cs.
app.MapDreams();
app.MapDreamLinks(SharePolicy);
app.MapImageFetch(FetchPolicy);
app.MapWallpaper(LinkPolicy);
app.MapBoardLink();
app.MapNudges();
app.Run();
return 0;

/// <summary>Named so the test host can reach it.</summary>
public partial class Program;
