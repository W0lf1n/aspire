using System.Text;
using Aspire.Domain;
using Aspire.Infrastructure.Media;

namespace Aspire.Api.Dreams;

/// <summary>
/// The page a shared dream is read on (D61) — the only HTML this server has
/// ever written, and the only screen in the app that is not the app.
///
/// It is a page rather than a route of the PWA for one reason: the person it
/// is sent to pastes the link into a message, and a message shows a preview
/// only if the *server* put the picture in the head. A client-rendered route
/// arrives at WhatsApp as a bare URL.
///
/// So it is deliberately one file with nothing in it: no script, no font, no
/// stylesheet, no analytics, no link back into the board, and a `noindex` in
/// the head because a private dream is not a page anybody should find by
/// searching. One photograph, one title, one line.
/// </summary>
public static class SharePage
{
    /// <summary>The card a chat app shows, which is its own JPEG (see the endpoint).</summary>
    public static string CardPathOf(string key) => $"/s/{key}/card.jpg";

    public static string Render(Dream dream, DreamImage? photo, string origin)
    {
        var title = Text(dream.Title);
        var line = dream.Affirmation.Length > 0 ? Text(dream.Affirmation) : string.Empty;
        // No photograph, no card: a preview with a picture that 404s looks
        // broken, where no preview at all looks deliberate.
        var card = dream.LinkKey is null || photo is null
            ? null
            : $"{origin}{CardPathOf(dream.LinkKey)}";
        var picture = photo is null ? null : $"{origin}{MediaStore.UrlOf(photo.DreamId, photo.Id, "screen")}";

        var head = new StringBuilder();
        head.Append($"<title>{title}</title>");
        // A dream somebody was sent is not a page for a search engine to hold
        // on to; `noindex` is the closest thing to asking for that back.
        head.Append("<meta name=\"robots\" content=\"noindex, nofollow\">");
        head.Append($"<meta property=\"og:title\" content=\"{title}\">");
        head.Append("<meta property=\"og:type\" content=\"website\">");
        if (line.Length > 0) head.Append($"<meta property=\"og:description\" content=\"{line}\">");
        if (card is not null)
        {
            head.Append($"<meta property=\"og:image\" content=\"{card}\">");
            head.Append("<meta property=\"og:image:width\" content=\"1200\">");
            head.Append("<meta property=\"og:image:height\" content=\"630\">");
            head.Append("<meta name=\"twitter:card\" content=\"summary_large_image\">");
        }

        var body = picture is null
            ? "<main class=\"sky\">"
            : $"<main style=\"background-image:url('{picture}')\">";

        // `$$` so the CSS keeps its own braces: with two dollars an
        // interpolation is `{{…}}` and a single brace is just a brace.
        return $$"""
            <!doctype html>
            <html lang="cs">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
            <meta name="theme-color" content="#12100e">
            {{head}}
            <style>
            *{box-sizing:border-box;margin:0}
            html,body{height:100%}
            body{background:#12100e;color:#fff;
            font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,system-ui,sans-serif;
            -webkit-font-smoothing:antialiased}
            main{position:relative;display:flex;align-items:flex-end;min-height:100%;
            padding:clamp(20px,6vw,56px);background-position:center;background-size:cover}
            main.sky{background-image:linear-gradient(160deg,#2a4d63,#123243)}
            main::before{content:'';position:absolute;inset:0;
            background:linear-gradient(to top,rgb(0 0 0/72%) 0%,rgb(0 0 0/34%) 38%,transparent 68%)}
            .body{position:relative;width:100%;max-width:34rem;margin:0 auto}
            h1{font-size:clamp(28px,7vw,44px);font-weight:600;line-height:1.12;letter-spacing:-.02em;
            text-wrap:balance}
            p{margin-top:.5em;font-size:clamp(15px,3.6vw,19px);line-height:1.45;
            color:rgb(255 255 255/78%);text-wrap:pretty}
            </style>
            </head>
            <body>
            {{body}}
            <div class="body">
            <h1>{{title}}</h1>
            {{(line.Length > 0 ? $"<p>{line}</p>" : string.Empty)}}
            </div>
            </main>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Somebody's own words, on a page. Every one of these came from a text
    /// field, so every one of them is escaped — a dream called
    /// <c>&lt;script&gt;</c> is a dream, not a tag — in element text and in a
    /// quoted attribute alike, which is why the quotes are in the set.
    ///
    /// These five and no more. <c>WebUtility.HtmlEncode</c> also turns every
    /// character above ASCII into a numeric entity, and this app is written in
    /// Czech: „Bydlím u lesa“ would go out as eight escapes and arrive looking
    /// the same, for twice the bytes. The page says <c>charset=utf-8</c>, so
    /// the letters can simply be letters.
    /// </summary>
    private static string Text(string value) => value
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'", "&#39;");
}
