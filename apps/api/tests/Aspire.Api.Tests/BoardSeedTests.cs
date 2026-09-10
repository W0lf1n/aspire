using Aspire.Api.Auth;
using Aspire.Api.Boards;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Api.Tests;

public sealed class BoardSeedTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public BoardSeedTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task An_empty_server_gets_its_first_board_from_the_code()
    {
        var seeded = await BoardSeed.ApplyAsync(_db, "000000");

        var board = await _db.Boards.SingleAsync();
        Assert.Equal(board.Id, seeded!.Id);
        Assert.Equal(BoardSeed.FirstBoardName, board.Name);
        Assert.True(PairingCode.Matches(board.CodeHash, "000000"));
    }

    [Fact]
    public async Task Without_a_code_the_first_board_exists_but_cannot_pair()
    {
        var seeded = await BoardSeed.ApplyAsync(_db, "  ");

        Assert.Null(seeded);
        var board = await _db.Boards.SingleAsync();
        Assert.Equal(string.Empty, board.CodeHash);
        Assert.False(PairingCode.Matches(board.CodeHash, ""));
    }

    [Fact]
    public async Task A_board_without_a_code_takes_the_configured_one()
    {
        // What the Boards migration leaves behind for rows that predate boards.
        _db.Boards.Add(new Board { Id = "legacy", Name = "Nástěnka" });
        await _db.SaveChangesAsync();

        var seeded = await BoardSeed.ApplyAsync(_db, "111111");

        Assert.Equal("legacy", seeded!.Id);
        Assert.True(PairingCode.Matches((await _db.Boards.SingleAsync()).CodeHash, "111111"));
    }

    [Fact]
    public async Task A_board_with_a_code_keeps_it()
    {
        var before = PairingCode.Hash("111111");
        _db.Boards.Add(new Board { Id = "b", Name = "B", CodeHash = before });
        await _db.SaveChangesAsync();

        var seeded = await BoardSeed.ApplyAsync(_db, "222222");

        Assert.Null(seeded);
        Assert.Equal(before, (await _db.Boards.SingleAsync()).CodeHash);
        Assert.Equal(1, await _db.Boards.CountAsync());
    }
}
