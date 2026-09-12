using Aspire.Api.Boards;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The key that is its own permission (D60). What matters here is what it
/// opens and what it stops opening: it is the one thing in this app that
/// reaches a board without a token.
/// </summary>
public sealed class BoardLinksTests : IDisposable
{
    private const string BoardA = "board-a";
    private const string BoardB = "board-b";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly BoardLinks _links;

    public BoardLinksTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Boards.Add(new Board { Id = BoardA, Name = "A" });
        _db.Boards.Add(new Board { Id = BoardB, Name = "B" });
        _db.SaveChanges();
        _links = new BoardLinks(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task A_board_starts_with_no_link_at_all()
    {
        Assert.Null(await _links.KeyOfAsync(BoardA));
    }

    [Fact]
    public async Task A_key_opens_the_board_it_was_made_for_and_no_other()
    {
        var key = await _links.MakeAsync(BoardA);

        Assert.Equal(BoardA, await _links.BoardOfAsync(key));
        Assert.Equal(key, await _links.KeyOfAsync(BoardA));
        Assert.Null(await _links.KeyOfAsync(BoardB));
    }

    [Fact]
    public async Task Making_one_again_replaces_it_which_is_what_revoking_looks_like()
    {
        var first = await _links.MakeAsync(BoardA);
        var second = await _links.MakeAsync(BoardA);

        Assert.NotEqual(first, second);
        Assert.Null(await _links.BoardOfAsync(first));
        Assert.Equal(BoardA, await _links.BoardOfAsync(second));
    }

    [Fact]
    public async Task A_deleted_link_opens_nothing_and_deleting_twice_is_fine()
    {
        var key = await _links.MakeAsync(BoardA);

        await _links.RevokeAsync(BoardA);
        await _links.RevokeAsync(BoardA);

        Assert.Null(await _links.BoardOfAsync(key));
        Assert.Null(await _links.KeyOfAsync(BoardA));
    }

    [Fact]
    public async Task Two_boards_with_no_link_do_not_collide_on_the_unique_index()
    {
        // Null is „no link“, and the index has to allow as many of those as
        // there are boards — the whole table is in that state to begin with.
        await _links.MakeAsync(BoardA);
        await _links.RevokeAsync(BoardA);

        Assert.Null(await _links.KeyOfAsync(BoardA));
        Assert.Null(await _links.KeyOfAsync(BoardB));
    }

    [Fact]
    public async Task Nothing_that_is_not_a_key_opens_anything()
    {
        await _links.MakeAsync(BoardA);

        Assert.Null(await _links.BoardOfAsync(""));
        Assert.Null(await _links.BoardOfAsync("not-a-key"));
        Assert.Null(await _links.BoardOfAsync(new string('x', 500)));
    }

    [Fact]
    public async Task A_key_is_long_random_and_safe_in_a_path()
    {
        var keys = new HashSet<string>();
        for (var i = 0; i < 50; i++) keys.Add(await _links.MakeAsync(BoardA));

        Assert.Equal(50, keys.Count);
        foreach (var key in keys)
        {
            // 32 bytes in base64url: 43 characters, none of them needing to be
            // escaped in a URL and none of them base64's two awkward ones.
            Assert.Equal(43, key.Length);
            Assert.DoesNotContain('=', key);
            Assert.DoesNotContain('+', key);
            Assert.DoesNotContain('/', key);
            Assert.Equal($"/api/v1/w/{key}", BoardLinks.PathOf(key));
        }
    }
}
