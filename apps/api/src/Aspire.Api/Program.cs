using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Aspire.Api;
using Aspire.Api.Auth;
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

// The pairing code is the only thing between the internet and the board.
// `DEPLOYMENT.md` generates twelve digits, because the code's own length is the
// only defence here that does not depend on a fence holding. `CodeMatches`
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
    IConfiguration config,
    DeviceAuth auth,
    CancellationToken ct) =>
{
    var expected = config["Pairing:Code"];
    if (string.IsNullOrWhiteSpace(expected))
    {
        // Refusing is the safe default: an unset code must never mean "any code
        // will do", which is how a private board becomes a public one.
        return Results.Problem("Párování není na serveru nastavené.", statusCode: 503);
    }

    if (!DeviceAuth.CodeMatches(expected, request.Code ?? string.Empty))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(await auth.PairAsync(request.DeviceName ?? "Zařízení", ct));
}).RequireRateLimiting(PairPolicy);

// The placeholder for M1: the board, in board order. Empty until there is a
// way to add a dream.
app.MapGet("/api/v1/dreams", async (
    HttpContext http,
    DeviceAuth auth,
    AppDbContext db,
    CancellationToken ct) =>
{
    var device = await auth.ResolveAsync(http.Request.Headers.Authorization, ct);
    if (device is null) return Results.Unauthorized();

    // The tie-break is the id, not `CreatedAt`: SQLite cannot order by a
    // DateTimeOffset, and the laptop mode has to run the same query.
    var dreams = await db.Dreams
        .OrderBy(d => d.SortOrder)
        .ThenBy(d => d.Id)
        .Select(d => DreamDto.From(d))
        .ToListAsync(ct);

    return Results.Ok(dreams);
});

app.Run();

/// <summary>Named so the test host can reach it.</summary>
public partial class Program;
