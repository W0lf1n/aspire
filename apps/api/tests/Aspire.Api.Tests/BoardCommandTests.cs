using Aspire.Api.Auth;
using Aspire.Api.Boards;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class BoardCommandTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly DeviceAuth _auth;
    private readonly StringWriter _out = new();

    public BoardCommandTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
        _auth = new DeviceAuth(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Task<int> Run(params string[] args) => BoardCommand.RunAsync(args, _db, _out);

    /// <summary>The code `invite` printed, read back off the screen.</summary>
    private string PrintedCode() =>
        _out.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .First(line => line.StartsWith("Pairing code: ", StringComparison.Ordinal))["Pairing code: ".Length..];

    [Fact]
    public async Task Invite_makes_a_board_that_pairs_with_the_code_it_printed()
    {
        var exit = await Run("board", "invite", "Zuzana");

        Assert.Equal(0, exit);
        var code = PrintedCode();
        Assert.Equal(BoardCommand.GeneratedCodeLength, code.Length);
        var board = await _auth.FindBoardAsync(code);
        Assert.Equal("Zuzana", board!.Name);
    }

    [Fact]
    public async Task Invite_makes_a_different_code_every_time()
    {
        await Run("board", "invite", "Zuzana");
        var first = PrintedCode();
        _out.GetStringBuilder().Clear();

        await Run("board", "invite", "Martin");

        Assert.NotEqual(first, PrintedCode());
    }

    [Fact]
    public async Task Invite_refuses_a_second_board_of_the_same_name()
    {
        await Run("board", "invite", "Zuzana");
        _out.GetStringBuilder().Clear();

        var exit = await Run("board", "invite", "Zuzana");

        Assert.Equal(1, exit);
        Assert.Equal(1, await _db.Boards.CountAsync());
        // Nothing to read: a refused invite must not look like a code.
        Assert.DoesNotContain("Pairing code:", _out.ToString());
    }

    [Fact]
    public async Task Add_makes_a_board_that_pairs()
    {
        var exit = await Run("board", "add", "Zuzana", "483920174635");

        Assert.Equal(0, exit);
        var board = await _auth.FindBoardAsync("483920174635");
        Assert.Equal("Zuzana", board!.Name);
        Assert.Contains("Zuzana", _out.ToString());
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdef")]
    [InlineData("1234 56")]
    public async Task Add_refuses_a_code_the_phone_could_not_type(string code)
    {
        var exit = await Run("board", "add", "Zuzana", code);

        Assert.Equal(1, exit);
        Assert.Equal(0, await _db.Boards.CountAsync());
    }

    [Fact]
    public async Task Add_refuses_a_second_board_of_the_same_name()
    {
        await Run("board", "add", "Zuzana", "483920174635");

        var exit = await Run("board", "add", " Zuzana ", "111111111111");

        Assert.Equal(1, exit);
        Assert.Equal(1, await _db.Boards.CountAsync());
    }

    [Fact]
    public async Task Code_changes_the_code_and_keeps_the_devices()
    {
        await Run("board", "add", "Zuzana", "483920174635");
        var board = await _auth.FindBoardAsync("483920174635");
        var paired = await _auth.PairAsync(board!, "Telefon");

        var exit = await Run("board", "code", "Zuzana", "209384756123");

        Assert.Equal(0, exit);
        Assert.Null(await _auth.FindBoardAsync("483920174635"));
        Assert.Equal(board!.Id, (await _auth.FindBoardAsync("209384756123"))!.Id);
        Assert.NotNull(await _auth.ResolveAsync($"Bearer {paired.Token}"));
    }

    [Fact]
    public async Task Code_names_a_board_that_is_not_there()
    {
        var exit = await Run("board", "code", "Nikdo", "209384756123");

        Assert.Equal(1, exit);
        Assert.Contains("Nikdo", _out.ToString());
    }

    [Fact]
    public async Task List_counts_devices_and_dreams()
    {
        await Run("board", "add", "Zuzana", "483920174635");
        var board = await _auth.FindBoardAsync("483920174635");
        await _auth.PairAsync(board!, "Telefon");
        _db.Dreams.Add(new Dream { Id = Guid.NewGuid(), BoardId = board!.Id, Title = "Dům u lesa" });
        await _db.SaveChangesAsync();

        var exit = await Run("board", "list");

        Assert.Equal(0, exit);
        Assert.Contains("Zuzana", _out.ToString());
        Assert.Contains("1 device(s), 1 dream(s), pairs", _out.ToString());
    }

    [Theory]
    [InlineData("board")]
    [InlineData("board", "add", "Zuzana")]
    [InlineData("board", "invite")]
    [InlineData("board", "invite", "Zuzana", "483920174635")]
    [InlineData("board", "drop", "Zuzana")]
    public async Task Anything_else_prints_the_usage(params string[] args)
    {
        var exit = await Run(args);

        Assert.Equal(2, exit);
        Assert.StartsWith("usage:", _out.ToString());
    }
}
