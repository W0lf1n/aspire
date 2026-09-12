using Aspire.Api.Dreams;
using Aspire.Domain;
using Aspire.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// A dream's share link (D61). The board's lock-screen key is the same shape
/// (<see cref="BoardLinksTests"/>); what is different here, and what these are
/// for, is that a key belongs to one dream and a device may only make one for
/// a dream on its own board.
/// </summary>
public sealed class DreamLinksTests : IDisposable
{
    private const string BoardA = "board-a";
    private const string BoardB = "board-b";

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly DreamLinks _links;

    public DreamLinksTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();
        _db.Boards.Add(new Board { Id = BoardA, Name = "A" });
        _db.Boards.Add(new Board { Id = BoardB, Name = "B" });
        _db.SaveChanges();
        _links = new DreamLinks(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Dream Add(string boardId, string title)
    {
        var dream = new Dream { Id = Guid.NewGuid(), BoardId = boardId, Title = title };
        _db.Dreams.Add(dream);
        _db.SaveChanges();
        return dream;
    }

    [Fact]
    public async Task A_dream_starts_unshared()
    {
        var dream = Add(BoardA, "Loď");

        Assert.Null(await _links.KeyOfAsync(BoardA, dream.Id));
    }

    [Fact]
    public async Task A_key_opens_the_dream_it_was_made_for_and_no_other()
    {
        var shared = Add(BoardA, "Loď");
        var other = Add(BoardA, "Dům");

        var key = await _links.MakeAsync(BoardA, shared.Id);

        Assert.NotNull(key);
        var found = await _links.FindAsync(key!);
        Assert.Equal(shared.Id, found!.Id);
        Assert.Null(await _links.KeyOfAsync(BoardA, other.Id));
    }

    [Fact]
    public async Task A_device_cannot_share_another_boards_dream()
    {
        var theirs = Add(BoardB, "Loď");

        // Not found rather than refused: a dream on another board does not
        // exist as far as this board is concerned, as everywhere else.
        Assert.Null(await _links.MakeAsync(BoardA, theirs.Id));
        Assert.Null(await _links.KeyOfAsync(BoardA, theirs.Id));
        Assert.Null(await _links.MakeAsync(BoardA, Guid.NewGuid()));
    }

    [Fact]
    public async Task Sharing_again_replaces_the_key_which_is_what_revoking_looks_like()
    {
        var dream = Add(BoardA, "Loď");

        var first = await _links.MakeAsync(BoardA, dream.Id);
        var second = await _links.MakeAsync(BoardA, dream.Id);

        Assert.NotEqual(first, second);
        Assert.Null(await _links.FindAsync(first!));
        Assert.Equal(dream.Id, (await _links.FindAsync(second!))!.Id);
    }

    [Fact]
    public async Task A_revoked_link_opens_nothing_and_revoking_twice_is_fine()
    {
        var dream = Add(BoardA, "Loď");
        var key = await _links.MakeAsync(BoardA, dream.Id);

        await _links.RevokeAsync(BoardA, dream.Id);
        await _links.RevokeAsync(BoardA, dream.Id);

        Assert.Null(await _links.FindAsync(key!));
        Assert.Null(await _links.KeyOfAsync(BoardA, dream.Id));
    }

    [Fact]
    public async Task Another_board_cannot_revoke_what_it_did_not_share()
    {
        var dream = Add(BoardA, "Loď");
        var key = await _links.MakeAsync(BoardA, dream.Id);

        await _links.RevokeAsync(BoardB, dream.Id);

        Assert.Equal(dream.Id, (await _links.FindAsync(key!))!.Id);
    }

    [Fact]
    public async Task Many_dreams_may_be_unshared_at_once_under_the_unique_index()
    {
        // Null is „not shared“, and nearly every dream on a board is in that
        // state — the index has to allow as many of them as there are dreams.
        Add(BoardA, "Loď");
        Add(BoardA, "Dům");
        var third = Add(BoardA, "Cesta");

        await _links.MakeAsync(BoardA, third.Id);
        await _links.RevokeAsync(BoardA, third.Id);

        Assert.Equal(3, await _db.Dreams.CountAsync(d => d.LinkKey == null));
    }

    [Fact]
    public async Task Nothing_that_is_not_a_key_opens_anything()
    {
        var dream = Add(BoardA, "Loď");
        await _links.MakeAsync(BoardA, dream.Id);

        Assert.Null(await _links.FindAsync(""));
        Assert.Null(await _links.FindAsync("not-a-key"));
        Assert.Null(await _links.FindAsync(new string('x', 500)));
    }

    [Fact]
    public async Task A_key_is_long_random_and_safe_in_a_path()
    {
        var dream = Add(BoardA, "Loď");
        var keys = new HashSet<string>();
        for (var i = 0; i < 50; i++) keys.Add((await _links.MakeAsync(BoardA, dream.Id))!);

        Assert.Equal(50, keys.Count);
        foreach (var key in keys)
        {
            Assert.Equal(43, key.Length);
            Assert.DoesNotContain('=', key);
            Assert.DoesNotContain('+', key);
            Assert.DoesNotContain('/', key);
            // Short, because this one is read off a screen and pasted into a
            // message rather than fetched by a machine.
            Assert.Equal($"/s/{key}", DreamLinks.PathOf(key));
        }
    }
}
