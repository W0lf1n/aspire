using Aspire.Api.Auth;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class DeviceAuthTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly DeviceAuth _auth;
    private readonly Board _board;

    public DeviceAuthTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        _auth = new DeviceAuth(_db);

        _board = new Board { Id = "board-a", Name = "A", CodeHash = PairingCode.Hash("123456") };
        _db.Boards.Add(_board);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Pairing_returns_a_token_and_never_stores_it()
    {
        var result = await _auth.PairAsync(_board, "Telefon");

        Assert.NotEmpty(result.Token);
        var stored = await _db.Devices.SingleAsync();
        // A database dump must not hand anybody a working device.
        Assert.DoesNotContain(result.Token, stored.TokenHash);
        Assert.Equal(DeviceAuth.Hash(result.Token), stored.TokenHash);
    }

    [Fact]
    public async Task Pairing_cuts_a_name_to_the_column()
    {
        var result = await _auth.PairAsync(_board, new string('n', 300));

        var stored = await _db.Devices.SingleAsync(d => d.Id == result.DeviceId);
        Assert.Equal(Device.NameMaxLength, stored.Name.Length);
    }

    [Theory]
    [InlineData("  Telefon  ", "Telefon")]
    [InlineData("", "Zařízení")]
    [InlineData("   ", "Zařízení")]
    [InlineData(null, "Zařízení")]
    public void A_name_is_trimmed_and_defaulted(string? given, string expected)
    {
        Assert.Equal(expected, DeviceAuth.Fit(given));
    }

    [Fact]
    public void The_cut_never_splits_an_emoji()
    {
        // 119 plain characters, then a two-unit emoji straddling the edge.
        var name = new string('n', Device.NameMaxLength - 1) + "\U0001F4AB" + "tail";

        var fitted = DeviceAuth.Fit(name);

        Assert.Equal(Device.NameMaxLength - 1, fitted.Length);
        Assert.False(char.IsHighSurrogate(fitted[^1]));
    }

    [Fact]
    public async Task A_token_resolves_to_its_device()
    {
        var paired = await _auth.PairAsync(_board, "Telefon");

        var device = await _auth.ResolveAsync($"Bearer {paired.Token}");

        Assert.NotNull(device);
        Assert.Equal(paired.DeviceId, device!.Id);
        Assert.NotNull(device.LastSeenAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer ")]
    [InlineData("Basic abc")]
    [InlineData("Bearer wrong-token")]
    public async Task Anything_else_resolves_to_nobody(string? header)
    {
        Assert.Null(await _auth.ResolveAsync(header));
    }

    [Fact]
    public void Two_tokens_are_never_the_same()
    {
        var tokens = Enumerable.Range(0, 200).Select(_ => DeviceAuth.NewToken()).ToHashSet();

        Assert.Equal(200, tokens.Count);
    }

    [Fact]
    public async Task A_paired_device_belongs_to_the_board_its_code_opened()
    {
        var paired = await _auth.PairAsync(_board, "Telefon");

        var stored = await _db.Devices.SingleAsync(d => d.Id == paired.DeviceId);
        Assert.Equal(_board.Id, stored.BoardId);
    }

    [Fact]
    public async Task The_code_opens_its_own_board_and_no_other()
    {
        _db.Boards.Add(new Board { Id = "board-b", Name = "B", CodeHash = PairingCode.Hash("654321") });
        await _db.SaveChangesAsync();

        Assert.Equal("board-a", (await _auth.FindBoardAsync("123456"))!.Id);
        Assert.Equal("board-b", (await _auth.FindBoardAsync("654321"))!.Id);
        Assert.Null(await _auth.FindBoardAsync("111111"));
        Assert.Null(await _auth.FindBoardAsync(""));
    }

    [Fact]
    public async Task A_server_whose_boards_have_no_code_refuses_to_pair()
    {
        Assert.True(await _auth.AnyBoardPairsAsync());

        _board.CodeHash = string.Empty;
        await _db.SaveChangesAsync();

        Assert.False(await _auth.AnyBoardPairsAsync());
        Assert.Null(await _auth.FindBoardAsync("123456"));
    }
}
