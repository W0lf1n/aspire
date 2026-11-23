using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Api.Auth;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>The board's devices, and taking one off it, over the wire (D93).</summary>
public sealed class DeviceEndpointsTests : IAsyncLifetime
{
    private readonly ApiFactory _api = new();
    private HttpClient _phone = null!;
    private HttpClient _laptop = null!;

    public async Task InitializeAsync()
    {
        _phone = await _api.PairedAsync();
        _laptop = await _api.PairedAsync();
    }

    public Task DisposeAsync()
    {
        _phone.Dispose();
        _laptop.Dispose();
        _api.Dispose();
        return Task.CompletedTask;
    }

    private static readonly JsonSerializerOptions Wire = new(JsonSerializerDefaults.Web);

    private static async Task<List<JsonElement>> DevicesOf(HttpClient client) =>
        (await client.GetFromJsonAsync<List<JsonElement>>("/api/v1/devices", Wire))!;

    private async Task<T> WithDb<T>(Func<AppDbContext, Task<T>> work)
    {
        using var scope = _api.Services.CreateScope();
        return await work(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    [Fact]
    public async Task The_list_is_the_boards_devices_with_the_asking_one_marked_and_no_token()
    {
        var devices = await DevicesOf(_phone);

        Assert.Equal(2, devices.Count);
        Assert.Equal([true, false], devices.Select(d => d.GetProperty("current").GetBoolean()));
        var names = devices[0].EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equal(["id", "name", "pairedAt", "lastSeenAt", "current"], names);
    }

    [Fact]
    public async Task A_device_that_disconnects_itself_is_refused_on_its_next_request()
    {
        Assert.Equal(HttpStatusCode.NoContent, (await _phone.DeleteAsync("/api/v1/device")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await _phone.GetAsync("/api/v1/dreams")).StatusCode);
        Assert.Single(await DevicesOf(_laptop));
    }

    [Fact]
    public async Task A_lost_phone_is_disconnected_from_another_device_with_its_mornings()
    {
        var phoneId = (await DevicesOf(_phone)).Single(d => d.GetProperty("current").GetBoolean())
            .GetProperty("id").GetString()!;
        await WithDb(async db =>
        {
            db.PushSubscriptions.Add(new PushSubscription
            {
                Id = Guid.NewGuid(),
                BoardId = (await db.Devices.SingleAsync(d => d.Id == phoneId)).BoardId,
                DeviceId = phoneId,
                Endpoint = "https://push.example/phone",
                P256dh = "p",
                Auth = "a"
            });
            return await db.SaveChangesAsync();
        });

        var response = await _laptop.DeleteAsync($"/api/v1/devices/{phoneId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _phone.GetAsync("/api/v1/dreams")).StatusCode);
        Assert.Equal(0, await WithDb(db => db.PushSubscriptions.CountAsync()));
    }

    [Fact]
    public async Task Another_boards_device_is_a_404_and_stays_paired()
    {
        const string token = "another-boards-token";
        await WithDb(async db =>
        {
            db.Boards.Add(new Board { Id = "other", Name = "Jiná" });
            db.Devices.Add(new Device
            {
                Id = "theirs", Name = "Jejich", BoardId = "other", TokenHash = DeviceAuth.Hash(token)
            });
            return await db.SaveChangesAsync();
        });

        Assert.Equal(HttpStatusCode.NotFound, (await _phone.DeleteAsync("/api/v1/devices/theirs")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _phone.DeleteAsync("/api/v1/devices/nobody")).StatusCode);
        Assert.Equal(2, (await DevicesOf(_phone)).Count);

        using var theirs = _api.CreateClient();
        theirs.DefaultRequestHeaders.Authorization = new("Bearer", token);
        Assert.Single(await DevicesOf(theirs));
    }

    [Fact]
    public async Task None_of_it_answers_without_a_token()
    {
        using var stranger = _api.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await stranger.GetAsync("/api/v1/devices")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await stranger.DeleteAsync("/api/v1/device")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await stranger.DeleteAsync("/api/v1/devices/x")).StatusCode);
    }
}
