using System.Net;
using System.Text.Json;
using Aspire.Api.Dreams;
using Aspire.Api.Images;
using Aspire.Api.Nudges;
using Aspire.Domain;
using Aspire.Infrastructure;
using Aspire.Infrastructure.Media;
using Aspire.Infrastructure.Push;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The morning, from the outside: the worker is given a board and a clock, and
/// what a phone would have shown is decrypted out of the request it made.
///
/// This is the only place the choice and the words are checked together, which
/// is the whole of D57 — on the one morning a year a dream has an anniversary,
/// the nudge is that instead of today's dream, and the day's pick is left
/// exactly where it was.
/// </summary>
public sealed class NudgeWorkerTests : IDisposable
{
    private const string Board = "board-a";
    private const string Endpoint = "https://push.example/device/abc";
    private const int Prague = 120;

    /// <summary>Seven in the morning in Prague, which is when the nudge is set for.</summary>
    private static readonly DateTimeOffset SevenAm = new(2026, 9, 12, 5, 0, 0, TimeSpan.Zero);

    // A real pair, so the body actually encrypts and can be read back.
    private const string VapidPublic =
        "BP4z9KsN6nGRTbVYI_c7VJSPQTBtkgcy27mlmlMoZIIgDll6e3vCYLocInmYWAmS6TlzAC8wEqKK6PBru3jl7A8";
    private const string VapidPrivate = "yfWPiYE-n46HLnH0KqZOF1fJJU3MYrct3AELtAQ-oRw";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly ServiceProvider _provider;
    private readonly Phone _phone = new();

    public NudgeWorkerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Boards.Add(new Board { Id = Board, Name = "A" });
        _db.SaveChanges();

        var services = new ServiceCollection();
        services.AddSingleton(_db);
        services.AddSingleton(new MediaStore(Path.Combine(Path.GetTempPath(), $"aspire-nudge-{Guid.NewGuid():N}")));
        services.AddSingleton<ImageQueue>();
        services.AddScoped<NudgeService>();
        services.AddScoped<DreamService>();
        services.AddScoped<ImageService>();
        _provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _provider.Dispose();
        _db.Dispose();
        _connection.Dispose();
    }

    private Dream Add(string title, DreamStatus status = DreamStatus.Dreaming, DateTimeOffset? achieved = null)
    {
        var dream = new Dream
        {
            Id = Guid.NewGuid(),
            BoardId = Board,
            Title = title,
            Status = status,
            AchievedAt = achieved,
            SortOrder = _db.Dreams.Count()
        };
        _db.Dreams.Add(dream);
        _db.SaveChanges();
        return dream;
    }

    private async Task SubscribeAsync()
    {
        await new NudgeService(_db).SaveAsync(Board, new NudgeInput(
            Endpoint,
            PushEnvelope.ReceiverPublic,
            PushEnvelope.AuthSecret,
            NudgeMode.Daily,
            7 * 60,
            Prague));
    }

    private Task RunAsync() => new NudgeWorker(
        _provider.GetRequiredService<IServiceScopeFactory>(),
        new VapidKeys(VapidPublic, VapidPrivate, "mailto:petr@example.com"),
        new WebPushSender(new HttpClient(_phone)),
        NullLogger<NudgeWorker>.Instance).SendDueAsync(SevenAm, default);

    /// <summary>
    /// The subscription as the table has it. Untracked, because the stamp is an
    /// <c>ExecuteUpdate</c> and the instance this test subscribed with would
    /// happily report the value it had before the worker ran (D51).
    /// </summary>
    private Task<PushSubscription> RowAsync() =>
        _db.PushSubscriptions.AsNoTracking().SingleAsync();

    /// <summary>What the phone was told, as its service worker would read it.</summary>
    private (string Title, string? Line, Guid Id) Nudge()
    {
        Assert.NotEmpty(_phone.Body);
        using var payload = JsonDocument.Parse(PushEnvelope.Open(_phone.Body));
        var root = payload.RootElement;
        return (
            root.GetProperty("title").GetString()!,
            root.GetProperty("line").GetString(),
            root.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task An_ordinary_morning_is_the_days_dream_and_stamps_it_shown()
    {
        var dream = Add("Dům u lesa");
        await SubscribeAsync();

        await RunAsync();

        var said = Nudge();
        Assert.Equal("Dům u lesa", said.Title);
        Assert.Equal(dream.Id, said.Id);
        // The board opens on the dream the person was already told about (D25).
        await _db.Entry(dream).ReloadAsync();
        Assert.NotNull(dream.LastShownAt);
    }

    [Fact]
    public async Task An_anniversary_replaces_the_days_dream_and_stamps_nothing()
    {
        var waiting = Add("Dům u lesa");
        var came = Add("Loď", DreamStatus.Achieved, new DateTimeOffset(2025, 9, 12, 10, 0, 0, TimeSpan.Zero));
        await SubscribeAsync();

        await RunAsync();

        var said = Nudge();
        Assert.Equal("Před rokem", said.Title);
        Assert.Equal("Splnil se ti sen „Loď“.", said.Line);
        // Tapping it opens the dream that came true, not today's.
        Assert.Equal(came.Id, said.Id);

        // The pick is untouched: the board still opens on its own dream, and
        // tomorrow's nudge is the one it would have been today (D57).
        await _db.Entry(waiting).ReloadAsync();
        Assert.Null(waiting.LastShownAt);
        Assert.Equal(
            DateOnly.FromDateTime(NudgeSchedule.LocalNow(SevenAm, Prague)),
            (await RowAsync()).LastSentOn);
    }

    [Fact]
    public async Task One_nudge_a_morning_however_many_dreams_have_anniversaries()
    {
        Add("Loď", DreamStatus.Achieved, new DateTimeOffset(2024, 9, 12, 10, 0, 0, TimeSpan.Zero));
        Add("Dům", DreamStatus.Achieved, new DateTimeOffset(2025, 9, 12, 10, 0, 0, TimeSpan.Zero));
        await SubscribeAsync();

        await RunAsync();

        Assert.Equal(1, _phone.Asked);
        // The most recently achieved of the two, as the wall is ordered.
        Assert.Equal("Před rokem", Nudge().Title);
    }

    [Fact]
    public async Task A_board_with_nothing_to_say_says_nothing_and_is_not_asked_again()
    {
        await SubscribeAsync();

        await RunAsync();

        Assert.Equal(0, _phone.Asked);
        Assert.NotNull((await RowAsync()).LastSentOn);
    }

    /// <summary>A push service that accepts everything, and keeps the last body.</summary>
    private sealed class Phone : HttpMessageHandler
    {
        public int Asked { get; private set; }
        public byte[] Body { get; private set; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            Asked++;
            Body = request.Content is null ? [] : await request.Content.ReadAsByteArrayAsync(ct);
            return new HttpResponseMessage(HttpStatusCode.Created);
        }
    }
}
