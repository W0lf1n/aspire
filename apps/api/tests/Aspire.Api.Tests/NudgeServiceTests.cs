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
        int[]? times = null,
        int? offset = 120) =>
        new(endpoint, "key-p256dh", "key-auth", mode, times ?? [7 * 60], offset);

    [Fact]
    public async Task Subscribing_twice_from_one_device_moves_the_row_it_has()
    {
        var first = await _nudges.SaveAsync(BoardA, Input());
        var again = await _nudges.SaveAsync(BoardA, Input(times: [6 * 60], mode: NudgeMode.Weekdays));

        Assert.Equal(first.Id, again.Id);
        Assert.Equal([6 * 60], again.Times);
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

        Assert.Equal(["https://push.example/prague"], due.Select(one => one.Subscription.Endpoint));
    }

    [Fact]
    public async Task A_device_marked_sent_is_not_due_again_until_tomorrow()
    {
        var utcNow = new DateTimeOffset(2026, 9, 10, 5, 0, 0, TimeSpan.Zero);
        var subscription = await _nudges.SaveAsync(BoardA, Input());

        Assert.Single(await _nudges.DueAsync(utcNow));

        await _nudges.MarkSentAsync(subscription, 7 * 60, utcNow);
        Assert.Empty(await _nudges.DueAsync(utcNow));

        // The next morning it is owed one again.
        Assert.Single(await _nudges.DueAsync(utcNow.AddDays(1)));
    }

    // ── several a day (D72) ────────────────────────────────────────────────

    [Fact]
    public async Task A_device_can_ask_for_up_to_five_times_and_gets_them_in_order()
    {
        var saved = await _nudges.SaveAsync(
            BoardA, Input(times: [18 * 60, 7 * 60, 12 * 60, 7 * 60]));

        // Sorted, and the repeat dropped: `DueAt` reads the list as
        // „everything up to here is done“.
        Assert.Equal([7 * 60, 12 * 60, 18 * 60], saved.Times);

        // And it survives the round trip through the column.
        var read = await _nudges.FindAsync(BoardA, Endpoint);
        Assert.Equal([7 * 60, 12 * 60, 18 * 60], read!.Times);
    }

    [Fact]
    public async Task Each_of_the_days_reminders_comes_due_in_its_turn()
    {
        // Prague, two hours ahead: 05:00 UTC is 07:00 there.
        var morning = new DateTimeOffset(2026, 9, 10, 5, 0, 0, TimeSpan.Zero);
        var subscription = await _nudges.SaveAsync(
            BoardA, Input(times: [7 * 60, 12 * 60], offset: 120));

        Assert.Equal(7 * 60, Assert.Single(await _nudges.DueAsync(morning)).AtMinutes);
        await _nudges.MarkSentAsync(subscription, 7 * 60, morning);
        Assert.Empty(await _nudges.DueAsync(morning));

        var noon = morning.AddHours(5);
        Assert.Equal(12 * 60, Assert.Single(await _nudges.DueAsync(noon)).AtMinutes);
        await _nudges.MarkSentAsync(subscription, 12 * 60, noon);
        Assert.Empty(await _nudges.DueAsync(noon));

        // And tomorrow the list starts again.
        Assert.Equal(7 * 60, Assert.Single(await _nudges.DueAsync(morning.AddDays(1))).AtMinutes);
    }

    [Fact]
    public void More_than_five_reminders_earns_a_sentence()
    {
        Assert.Equal(
            $"Víc než {PushSubscription.MaxTimes} připomenutí denně nejde.",
            NudgeService.Problem(Input(times: [0, 60, 120, 180, 240, 300])));
    }

    [Fact]
    public void A_time_that_is_not_one_earns_a_sentence()
    {
        Assert.Equal("Tenhle čas neznám.", NudgeService.Problem(Input(times: [24 * 60])));
        Assert.Equal("Tenhle čas neznám.", NudgeService.Problem(Input(times: [420, 420])));
        Assert.Equal("Tenhle čas neznám.", NudgeService.Problem(Input(times: [])));
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
            NudgeService.Problem(new NudgeInput(Endpoint, null, null, NudgeMode.Daily, [420], 0)));
    }

    [Fact]
    public void A_time_or_a_zone_it_does_not_know_earns_a_sentence()
    {
        Assert.Equal("Tenhle čas neznám.", NudgeService.Problem(Input(times: [24 * 60])));
        Assert.Equal("Tohle časové pásmo neznám.", NudgeService.Problem(Input(offset: 15 * 60)));
        Assert.Null(NudgeService.Problem(Input()));
    }

    // ── the offset, reported every time the app opens (D51) ─────────────────

    [Fact]
    public async Task Opening_the_app_somewhere_else_moves_the_offset_and_nothing_else()
    {
        await _nudges.SaveAsync(BoardA, Input(mode: NudgeMode.Weekdays, times: [6 * 60], offset: 120));

        Assert.True(await _nudges.UpdateOffsetAsync(BoardA, Endpoint, -180));

        var row = await _nudges.FindAsync(BoardA, Endpoint);
        Assert.NotNull(row);
        Assert.Equal(-180, row.UtcOffsetMinutes);
        // The hours and the mode are the person's; a report of where the
        // phone is must never overwrite either of them.
        Assert.Equal([6 * 60], row.Times);
        Assert.Equal(NudgeMode.Weekdays, row.Mode);
    }

    [Fact]
    public async Task A_moved_offset_decides_the_next_morning()
    {
        // Subscribed in Prague, wanting seven: at 05:00 UTC it is owed one.
        await _nudges.SaveAsync(BoardA, Input(offset: 120));
        var utcNow = new DateTimeOffset(2026, 9, 12, 5, 0, 0, TimeSpan.Zero);
        Assert.Single(await _nudges.DueAsync(utcNow));

        // The same phone opens the app in Lisbon. Seven there is not yet.
        await _nudges.UpdateOffsetAsync(BoardA, Endpoint, -180);

        Assert.Empty(await _nudges.DueAsync(utcNow));
        Assert.Single(await _nudges.DueAsync(utcNow.AddHours(5)));
    }

    [Fact]
    public async Task A_device_that_never_asked_to_be_nudged_has_no_offset_to_keep()
    {
        Assert.False(await _nudges.UpdateOffsetAsync(BoardA, Endpoint, 60));
        Assert.Empty(await _db.PushSubscriptions.ToListAsync());
    }

    [Fact]
    public async Task One_board_cannot_move_another_boards_offset()
    {
        await _nudges.SaveAsync(BoardA, Input(offset: 120));

        Assert.False(await _nudges.UpdateOffsetAsync(BoardB, Endpoint, -180));

        var row = await _nudges.FindAsync(BoardA, Endpoint);
        Assert.Equal(120, row!.UtcOffsetMinutes);
    }

    [Theory]
    [InlineData(null, 120, "Prohlížeč nedal adresu pro upozornění.")]
    [InlineData(Endpoint, null, "Tohle časové pásmo neznám.")]
    [InlineData(Endpoint, 15 * 60, "Tohle časové pásmo neznám.")]
    public void An_offset_report_that_makes_no_sense_earns_a_sentence(
        string? endpoint, int? offset, string expected)
    {
        Assert.Equal(expected, NudgeService.Problem(new NudgeOffsetInput(endpoint, offset)));
    }

    [Fact]
    public void An_offset_report_that_makes_sense_earns_nothing()
    {
        Assert.Null(NudgeService.Problem(new NudgeOffsetInput(Endpoint, -180)));
    }
}
