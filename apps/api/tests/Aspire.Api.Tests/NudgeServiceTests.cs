using Aspire.Api;
using Aspire.Api.Nudges;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class NudgeServiceTests : IDisposable
{
    private const string BoardA = "board-a";
    private const string BoardB = "board-b";
    private const string Endpoint = "https://push.example/abc";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly NudgeService _nudges;

    public NudgeServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Boards.Add(new Board { Id = BoardA, Name = "A" });
        _db.Boards.Add(new Board { Id = BoardB, Name = "B" });
        _db.SaveChanges();
        _nudges = new NudgeService(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static NudgeInput Input(
        string endpoint = Endpoint,
        NudgeMode? mode = NudgeMode.Daily,
        int? at = 7 * 60,
        int? offset = 120) => new(endpoint, "key-p256dh", "key-auth", mode, at, offset);

    [Fact]
    public async Task Subscribing_twice_from_one_device_moves_the_row_it_has()
    {
        var first = await _nudges.SaveAsync(BoardA, Input());
        var again = await _nudges.SaveAsync(BoardA, Input(at: 6 * 60, mode: NudgeMode.Weekdays));

        Assert.Equal(first.Id, again.Id);
        Assert.Equal(6 * 60, again.AtMinutes);
        Assert.Equal(NudgeMode.Weekdays, again.Mode);
        Assert.Equal(1, await _db.PushSubscriptions.CountAsync());
    }

    [Fact]
    public async Task A_phone_re_paired_into_another_board_moves_rather_than_collides()
    {
        await _nudges.SaveAsync(BoardA, Input());
        var moved = await _nudges.SaveAsync(BoardB, Input());

        Assert.Equal(BoardB, moved.BoardId);
        Assert.Equal(1, await _db.PushSubscriptions.CountAsync());
        Assert.Null(await _nudges.FindAsync(BoardA, Endpoint));
    }

    [Fact]
    public async Task Turning_it_off_leaves_no_row()
    {
        await _nudges.SaveAsync(BoardA, Input());

        Assert.True(await _nudges.RemoveAsync(BoardA, Endpoint));
        Assert.Empty(await _db.PushSubscriptions.ToListAsync());
        Assert.False(await _nudges.RemoveAsync(BoardA, Endpoint));
    }

    [Fact]
    public async Task One_board_cannot_reach_another_boards_subscription()
    {
        await _nudges.SaveAsync(BoardA, Input());

        Assert.Null(await _nudges.FindAsync(BoardB, Endpoint));
        Assert.False(await _nudges.RemoveAsync(BoardB, Endpoint));
        Assert.NotNull(await _nudges.FindAsync(BoardA, Endpoint));
    }

    [Fact]
    public async Task Due_reads_each_devices_own_clock()
    {
        // Both want seven in the morning; they are five hours apart, so at
        // 05:00 UTC only the one two hours ahead is owed it.
        await _nudges.SaveAsync(BoardA, Input(endpoint: "https://push.example/prague", offset: 120));
        await _nudges.SaveAsync(BoardB, Input(endpoint: "https://push.example/lisbon", offset: -180));

        var due = await _nudges.DueAsync(new DateTimeOffset(2026, 9, 10, 5, 0, 0, TimeSpan.Zero));

        Assert.Equal(["https://push.example/prague"], due.Select(s => s.Endpoint));
    }

    [Fact]
    public async Task A_device_marked_sent_is_not_due_again_until_tomorrow()
    {
        var utcNow = new DateTimeOffset(2026, 9, 10, 5, 0, 0, TimeSpan.Zero);
        var subscription = await _nudges.SaveAsync(BoardA, Input());

        Assert.Single(await _nudges.DueAsync(utcNow));

        await _nudges.MarkSentAsync(subscription, utcNow);
        Assert.Empty(await _nudges.DueAsync(utcNow));

        // The next morning it is owed one again.
        Assert.Single(await _nudges.DueAsync(utcNow.AddDays(1)));
    }

    [Fact]
    public async Task A_subscription_the_push_service_says_is_gone_is_forgotten()
    {
        var subscription = await _nudges.SaveAsync(BoardA, Input());

        await _nudges.ForgetAsync(subscription.Id);

        Assert.Empty(await _db.PushSubscriptions.ToListAsync());
    }

    [Theory]
    [InlineData(null, "Prohlížeč nedal adresu pro upozornění.")]
    [InlineData("   ", "Prohlížeč nedal adresu pro upozornění.")]
    public void A_subscription_without_an_endpoint_earns_a_sentence(string? endpoint, string expected)
    {
        Assert.Equal(expected, NudgeService.Problem(Input(endpoint!)));
    }

    [Fact]
    public void A_subscription_without_keys_earns_a_sentence()
    {
        Assert.Equal(
            "Prohlížeč nedal klíče pro upozornění.",
            NudgeService.Problem(new NudgeInput(Endpoint, null, null, NudgeMode.Daily, 420, 0)));
    }

    [Fact]
    public void A_time_or_a_zone_it_does_not_know_earns_a_sentence()
    {
        Assert.Equal("Tenhle čas neznám.", NudgeService.Problem(Input(at: 24 * 60)));
        Assert.Equal("Tohle časové pásmo neznám.", NudgeService.Problem(Input(offset: 15 * 60)));
        Assert.Null(NudgeService.Problem(Input()));
    }
}
