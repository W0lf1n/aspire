using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The endpoints as a phone meets them: a real request, a real status code,
/// the real JSON (D69). What is checked here is only what the road adds —
/// the header, the code, the casing, the multipart — because the rules
/// behind it have their own tests and do not need a second set.
/// </summary>
public sealed class HttpEndpointTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync() => _client = await _api.PairedAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _api.Dispose();
        return Task.CompletedTask;
    }

    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);

    private static StringContent Json(string body) => new(body, System.Text.Encoding.UTF8, "application/json");

    /// <summary>A dream with diacritics in every field, made over the wire.</summary>
    private async Task<JsonElement> ADreamAsync(string title = "Chci vidět Island")
    {
        var response = await _client.PostAsync("/api/v1/dreams", Json($$"""
            {"title":"{{title}}","why":"Protože přesně tam chci být.","affirmation":"Stojím na útesu.",
             "status":"in-progress","category":"want","targetYear":2027}
            """));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Wire);
    }

    [Fact]
    public async Task Health_answers_without_a_token()
    {
        using var anonymous = _api.CreateClient();

        var response = await anonymous.GetAsync("/api/v1/health");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Wire);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("ok").GetBoolean());
    }

    [Fact]
    public async Task Everything_else_needs_the_bearer_token()
    {
        using var anonymous = _api.CreateClient();

        foreach (var path in new[] { "/api/v1/dreams", "/api/v1/board", "/api/v1/board/link" })
        {
            var response = await anonymous.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task A_token_the_server_does_not_know_is_unauthorized_not_a_500()
    {
        using var stranger = _api.CreateClient();
        stranger.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");

        var response = await stranger.GetAsync("/api/v1/dreams");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_pairing_code_opens_nothing()
    {
        using var anonymous = _api.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/v1/pair", new { code = "999999", deviceName = "Cizí" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_dream_travels_camelCase_with_kebab_case_enums_and_its_diacritics_intact()
    {
        var dream = await ADreamAsync();

        // The casing the TypeScript client is written against (rule 6).
        Assert.Equal("Chci vidět Island", dream.GetProperty("title").GetString());
        Assert.Equal("Protože přesně tam chci být.", dream.GetProperty("why").GetString());
        Assert.Equal("in-progress", dream.GetProperty("status").GetString());
        Assert.Equal("want", dream.GetProperty("category").GetString());
        Assert.Equal(2027, dream.GetProperty("targetYear").GetInt32());
        Assert.Equal(0, dream.GetProperty("likes").GetInt32());
        Assert.True(dream.TryGetProperty("focusRank", out var rank));
        Assert.Equal(JsonValueKind.Null, rank.ValueKind);
        Assert.Empty(dream.GetProperty("images").EnumerateArray());
    }

    [Fact]
    public async Task A_dream_is_read_back_changed_and_deleted_over_the_wire()
    {
        var made = await ADreamAsync();
        var id = made.GetProperty("id").GetString();

        var read = await _client.GetFromJsonAsync<JsonElement>($"/api/v1/dreams/{id}", Wire);
        Assert.Equal("Chci vidět Island", read.GetProperty("title").GetString());

        var changed = await _client.PutAsync($"/api/v1/dreams/{id}", Json("""
            {"title":"Island na kole","why":"Přes celý ostrov.","affirmation":"",
             "status":"achieved","category":"do","targetYear":null}
            """));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var after = await changed.Content.ReadFromJsonAsync<JsonElement>(Wire);
        Assert.Equal("achieved", after.GetProperty("status").GetString());
        Assert.Equal("Island na kole", after.GetProperty("title").GetString());

        var gone = await _client.DeleteAsync($"/api/v1/dreams/{id}");
        Assert.Equal(HttpStatusCode.NoContent, gone.StatusCode);

        var missing = await _client.GetAsync($"/api/v1/dreams/{id}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task A_dream_that_is_not_on_this_board_is_a_404_and_never_a_500()
    {
        var response = await _client.GetAsync($"/api/v1/dreams/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_dream_with_nothing_in_it_is_a_400_with_a_sentence()
    {
        var response = await _client.PostAsync("/api/v1/dreams", Json("""
            {"title":"","why":"","affirmation":"","status":"dreaming","category":null,"targetYear":null}
            """));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Wire);
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("detail").GetString()));
    }

    [Fact]
    public async Task The_eleventh_dream_on_Ted_is_a_409_and_not_a_400()
    {
        // Ten fit; the cap is the feature rather than a limit on it (D53).
        for (var i = 1; i <= 10; i++)
        {
            var one = await ADreamAsync($"Sen {i}");
            var added = await _client.PostAsync($"/api/v1/dreams/{one.GetProperty("id").GetString()}/focus", null);
            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        }

        var eleventh = await ADreamAsync("Jedenáctý");
        var refused = await _client.PostAsync(
            $"/api/v1/dreams/{eleventh.GetProperty("id").GetString()}/focus", null);

        Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>(Wire);
        Assert.Equal(
            $"Na teď máš už {Domain.Dream.FocusMax} snů. Některý nejdřív odeber.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task The_board_says_what_it_holds_against_the_ceiling()
    {
        await ADreamAsync();

        var board = await _client.GetFromJsonAsync<JsonElement>("/api/v1/board", Wire);

        Assert.False(string.IsNullOrWhiteSpace(board.GetProperty("name").GetString()));
        Assert.Equal(0, board.GetProperty("photographs").GetInt32());
        Assert.Equal(0, board.GetProperty("bytes").GetInt64());
        Assert.Equal(Images.ImageService.MaxBoardBytes, board.GetProperty("bytesLimit").GetInt64());
    }

    [Fact]
    public async Task A_photograph_goes_up_as_multipart_and_comes_back_as_three_URLs()
    {
        var dream = await ADreamAsync();
        var id = dream.GetProperty("id").GetString();

        using var form = new MultipartFormDataContent();
        var png = Png(1600, 1000);
        var file = new ByteArrayContent(png);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        // The name the endpoint binds `IFormFile file` from, and a filename,
        // because a part without one is not a file to the model binder.
        form.Add(file, "file", "dream.png");

        var response = await _client.PostAsync($"/api/v1/dreams/{id}/images?kind=dreamt&focusX=0.25&focusY=0.75", form);

        // 202: the row is there, the sizes follow (D23).
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var image = await response.Content.ReadFromJsonAsync<JsonElement>(Wire);
        Assert.False(image.GetProperty("ready").GetBoolean());
        Assert.Equal(0.25, image.GetProperty("focusX").GetDouble(), 3);
        Assert.Equal(0.75, image.GetProperty("focusY").GetDouble(), 3);
        Assert.EndsWith("thumb.webp", image.GetProperty("thumbUrl").GetString());
        Assert.EndsWith("screen.webp", image.GetProperty("screenUrl").GetString());
        // The 2048 rung is `largeUrl` on the wire and `full` on disk (D62).
        Assert.EndsWith("full.webp", image.GetProperty("largeUrl").GetString());
    }

    [Fact]
    public async Task Anything_that_is_not_a_picture_is_refused_with_a_sentence()
    {
        var dream = await ADreamAsync();

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("not a photograph"u8.ToArray()), "file", "dream.png");

        var response = await _client.PostAsync(
            $"/api/v1/dreams/{dream.GetProperty("id").GetString()}/images", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Wire);
        Assert.Equal("Tohle není obrázek.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task A_crop_outside_the_photograph_is_refused_before_anything_is_stored()
    {
        var dream = await ADreamAsync();

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Png(64, 64));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", "dream.png");

        var response = await _client.PostAsync(
            $"/api/v1/dreams/{dream.GetProperty("id").GetString()}/images?focusX=1.5", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Wire);
        Assert.Equal("Výřez je mimo fotku.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task A_device_that_never_subscribed_reads_off_with_one_time_as_an_array()
    {
        var nudge = await _client.GetFromJsonAsync<JsonElement>("/api/v1/nudge", Wire);

        Assert.Equal("off", nudge.GetProperty("mode").GetString());
        // An array on the wire from the first read, so a screen never has to
        // tell a number from a list of them (D72, rule 6).
        var times = nudge.GetProperty("times");
        Assert.Equal(JsonValueKind.Array, times.ValueKind);
        Assert.Equal([Domain.PushSubscription.DefaultAtMinutes], times.EnumerateArray().Select(one => one.GetInt32()));
    }

    [Fact]
    public async Task A_board_with_no_link_says_so_rather_than_inventing_one()
    {
        var link = await _client.GetFromJsonAsync<JsonElement>("/api/v1/board/link", Wire);

        Assert.Equal(JsonValueKind.Null, link.GetProperty("path").ValueKind);
    }

    [Fact]
    public async Task A_key_that_opens_nothing_is_a_bare_404()
    {
        using var anonymous = _api.CreateClient();

        foreach (var path in new[] { "/api/v1/w/nothing-at-all", "/s/nothing-at-all" })
        {
            var response = await anonymous.GetAsync(path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    /// <summary>A PNG of the given size, as bytes, for a multipart part.</summary>
    private static byte[] Png(int width, int height)
    {
        using var image = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(
            width, height, new SixLabors.ImageSharp.PixelFormats.Rgba32(90, 79, 214));
        using var stream = new MemoryStream();
        image.Save(stream, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
        return stream.ToArray();
    }
}
