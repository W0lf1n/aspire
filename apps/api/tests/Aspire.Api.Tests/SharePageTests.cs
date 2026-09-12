using Aspire.Api.Dreams;
using Aspire.Domain;
using Xunit;

namespace Aspire.Api.Tests;

/// <summary>
/// The one page this server writes (D61). Two things matter about it: a chat
/// app has to find the picture in the head, and the words on it came out of a
/// text field — so a dream whose title is a tag is a title, not a tag.
/// </summary>
public sealed class SharePageTests
{
    private const string Origin = "https://sny.example";
    private const string Key = "CMy_KM9TpYsg0DZl5X_3fr66QnVrAPBxVc7qU6ifdqg";

    private static Dream Dream(string title = "Dům u lesa", string affirmation = "", string? key = Key) =>
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            BoardId = "board",
            Title = title,
            Affirmation = affirmation,
            LinkKey = key
        };

    private static DreamImage Photo() =>
        new()
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DreamId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Kind = DreamImageKind.Dreamt,
            ProcessedAt = DateTimeOffset.UtcNow
        };

    [Fact]
    public void The_dream_is_the_page_and_the_card_is_in_the_head()
    {
        var html = SharePage.Render(Dream(affirmation: "Bydlím u lesa"), Photo(), Origin);

        Assert.Contains("<title>Dům u lesa</title>", html);
        Assert.Contains("<h1>Dům u lesa</h1>", html);
        Assert.Contains("<p>Bydlím u lesa</p>", html);
        // Absolute, because Open Graph will not resolve a relative one.
        Assert.Contains($"property=\"og:image\" content=\"{Origin}/s/{Key}/card.jpg\"", html);
        Assert.Contains("name=\"twitter:card\" content=\"summary_large_image\"", html);
        Assert.Contains($"url('{Origin}/media/", html);
    }

    [Fact]
    public void Somebodys_own_words_are_words_and_never_markup()
    {
        var html = SharePage.Render(
            Dream("<script>alert(1)</script>", "\"ahoj\" & <b>tuc</b>"),
            null,
            Origin);

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<b>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&amp;", html);
        // And the same words in the head, where a broken quote would end the
        // attribute and start an element.
        Assert.DoesNotContain("content=\"\"ahoj\"", html);
        Assert.Contains("&quot;", html);
    }

    [Fact]
    public void A_dream_with_no_line_says_only_its_name()
    {
        var html = SharePage.Render(Dream(), Photo(), Origin);

        Assert.DoesNotContain("<p>", html);
        Assert.DoesNotContain("og:description", html);
    }

    [Fact]
    public void A_dream_with_no_photograph_is_still_a_page()
    {
        var html = SharePage.Render(Dream(), null, Origin);

        Assert.Contains("<main class=\"sky\">", html);
        // Nothing to preview, so nothing claimed: a card with a missing
        // picture looks broken where no card at all looks deliberate.
        Assert.DoesNotContain("og:image", html);
    }

    [Fact]
    public void It_asks_not_to_be_indexed_and_carries_nothing_that_runs()
    {
        var html = SharePage.Render(Dream(affirmation: "Bydlím u lesa"), Photo(), Origin);

        Assert.Contains("name=\"robots\" content=\"noindex, nofollow\"", html);
        Assert.DoesNotContain("<script", html);
        // One file: no stylesheet, no font, nothing fetched from anywhere else.
        Assert.DoesNotContain("<link", html);
        Assert.Contains("<html lang=\"cs\">", html);
    }
}
