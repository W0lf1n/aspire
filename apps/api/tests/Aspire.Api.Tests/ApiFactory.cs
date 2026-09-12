using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Aspire.Api.Tests;

/// <summary>
/// The whole server, over HTTP, on a database and a media root of its own
/// (D69).
///
/// Everything else in this project reaches past the request: it calls a
/// service with a <c>DbContext</c> the test made. That checks the rule and
/// not the road to it — the auth header, the status code a problem comes
/// back as, the multipart the phone actually sends, the JSON casing the
/// client is written against. All of that is wiring only a real request
/// touches, and it has been wrong before.
///
/// It runs the laptop's own configuration: SQLite, a file per factory, the
/// media root in the temp directory, and the pairing code <c>000000</c> that
/// <see cref="Boards.BoardSeed"/> puts on the first board. Nothing here talks
/// to Postgres and nothing leaves a trace outside its own two temporary
/// paths.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"aspire-http-{Guid.NewGuid():N}");

    /// <summary>The code a device pairs with, as the laptop's configuration has it.</summary>
    public const string Code = "000000";

    private string Database => Path.Combine(_root, "aspire.db");

    public string MediaRoot => Path.Combine(_root, "media");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(MediaRoot);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "sqlite",
                ["ConnectionStrings:Sqlite"] = $"Data Source={Database}",
                ["Media:Root"] = MediaRoot,
                ["Pairing:Code"] = Code,
                // No key pair, so the nudge worker stops itself rather than
                // trying to reach a push service from a test run.
                ["Push:PublicKey"] = "",
                ["Push:PrivateKey"] = ""
            });
        });
    }

    /// <summary>A client carrying a freshly paired device's token.</summary>
    public async Task<HttpClient> PairedAsync()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/pair", new { code = Code, deviceName = "Test" });
        response.EnsureSuccessStatusCode();

        var paired = await response.Content.ReadFromJsonAsync<PairedDevice>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", paired!.Token);
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
            // The SQLite file can still be held on Windows a moment after the
            // host goes. A temp directory left behind is not worth failing a
            // green run over.
        }
    }

    private sealed record PairedDevice(string DeviceId, string Token);
}
