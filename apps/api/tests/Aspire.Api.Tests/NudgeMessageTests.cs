using System.Text.Json;
using Aspire.Api.Nudges;
using Aspire.Domain;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The Czech a phone shows at seven in the morning, and which of a dream's two
/// photographs goes with it. This is the one string in the app nobody can
/// proof-read on a screen before it is sent.
/// </summary>
public sealed class NudgeMessageTests
{
    private static readonly Guid DreamId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Dream Dream(string title = "Dům u lesa", string affirmation = "", string why = "") =>
        new() { Id = DreamId, BoardId = "board", Title = title, Affirmation = affirmation, Why = why };

    private static DreamImage Photo(DreamImageKind kind, bool ready = true) =>
        new()
        {
            Id = Guid.NewGuid(),
            DreamId = DreamId,
            Kind = kind,
            ProcessedAt = ready ? DateTimeOffset.UtcNow : null
        };

    private static (string? Title, string? Line, string? Image) Read(string json)
    {
        using var payload = JsonDocument.Parse(json);
        var root = payload.RootElement;
        Assert.Equal(DreamId, root.GetProperty("id").GetGuid());
        return (
            root.GetProperty("title").GetString(),
            root.GetProperty("line").GetString(),
            root.GetProperty("image").GetString());
    }

    [Fact]
    public void Todays_dream_is_headed_by_its_own_title_and_says_its_affirmation()
    {
        var said = Read(NudgeMessage.For(Dream(affirmation: "Bydlím u lesa", why: "Chci klid"), []));

        Assert.Equal("Dům u lesa", said.Title);
        // The affirmation over the why, as the tile does it (D27).
        Assert.Equal("Bydlím u lesa", said.Line);
    }

    [Fact]
    public void A_dream_with_neither_line_is_still_worth_waking_for()
    {
        var said = Read(NudgeMessage.For(Dream(), []));

        Assert.Equal("Dům u lesa", said.Title);
        Assert.Null(said.Line);
    }

    [Theory]
    [InlineData(1, "Před rokem")]
    [InlineData(2, "Před 2 lety")]
    [InlineData(11, "Před 11 lety")]
    public void The_anniversary_is_headed_by_how_long_ago(int years, string heading)
    {
        var said = Read(NudgeMessage.ForAnniversary(Dream(), years, []));

        // The two lines read as one sentence, the way `formatAnniversary`
        // says it on the board.
        Assert.Equal(heading, said.Title);
        Assert.Equal("Splnil se ti sen „Dům u lesa“.", said.Line);
    }

    [Fact]
    public void Todays_dream_shows_the_dreamt_photograph_and_never_the_proof()
    {
        var dreamt = Photo(DreamImageKind.Dreamt);

        Assert.Contains(dreamt.Id.ToString(), Read(NudgeMessage.For(Dream(), [Photo(DreamImageKind.Achieved), dreamt])).Image);
        // A dream whose only photograph is the proof is not the board's
        // picture: the two are never interchangeable (D28).
        Assert.Null(Read(NudgeMessage.For(Dream(), [Photo(DreamImageKind.Achieved)])).Image);
    }

    [Fact]
    public void The_anniversary_shows_the_proof_and_the_dreamt_one_when_there_is_none()
    {
        var dreamt = Photo(DreamImageKind.Dreamt);
        var achieved = Photo(DreamImageKind.Achieved);

        Assert.Contains(achieved.Id.ToString(), Read(NudgeMessage.ForAnniversary(Dream(), 1, [dreamt, achieved])).Image);
        Assert.Contains(dreamt.Id.ToString(), Read(NudgeMessage.ForAnniversary(Dream(), 1, [dreamt])).Image);
    }

    [Fact]
    public void A_photograph_still_being_resized_is_not_a_photograph()
    {
        // The row exists from the upload and the sizes follow a moment later;
        // a notification must never point at a file that is not there.
        Assert.Null(Read(NudgeMessage.For(Dream(), [Photo(DreamImageKind.Dreamt, ready: false)])).Image);
    }
}
