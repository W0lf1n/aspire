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

        // The photograph as a picture rather than as a background, so it keeps
        // its own proportions instead of being stretched to whatever shape the
        // window happens to be — and so it can be cropped where the person
        // cropped it (rule 16).
        var frame = photo is null
            ? "<div class=\"frame frame--sky\"></div>"
            : $"<div class=\"frame\"><img class=\"shot\" src=\"{picture}\" alt=\"\" " +
              $"style=\"{FocalStyle(photo)}\" decoding=\"async\"></div>";

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
            html{height:100%}
            body{min-height:100%;display:flex;flex-direction:column;align-items:center;
            justify-content:center;gap:14px;
            padding:clamp(16px,4vw,40px) clamp(16px,4vw,40px) calc(clamp(16px,4vw,40px) + env(safe-area-inset-bottom,0px));
            background:#12100e;color:#fff;
            font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,system-ui,sans-serif;
            -webkit-font-smoothing:antialiased}
            /* A phone-shaped card in the middle of whatever window this is,
               which is what the app itself is on a desktop. */
            .card{position:relative;width:100%;max-width:25rem;border-radius:20px;overflow:hidden;
            background:#1b1714;box-shadow:0 24px 64px rgb(0 0 0/55%)}
            .frame{position:relative;overflow:hidden;aspect-ratio:4/5}
            .frame--sky{background:linear-gradient(160deg,#e5602a,#d84b7a 48%,#5a4fd6)}
            .shot{display:block;width:100%;height:100%;object-fit:cover}
            /* The words at the foot over a scrim, the way a dream reads on the
               board. Only the bottom: the top of a photograph stays one. */
            .body{position:absolute;left:0;right:0;bottom:0;padding:22px 20px 20px}
            .body::before{content:'';position:absolute;inset:-64px 0 0;
            background:linear-gradient(to top,rgb(0 0 0/78%) 0%,rgb(0 0 0/40%) 46%,transparent 100%);
            pointer-events:none}
            h1{position:relative;font-size:clamp(24px,5.6vw,32px);font-weight:600;line-height:1.14;
            letter-spacing:-.02em;text-wrap:balance}
            .line{position:relative;margin-top:.4em;font-size:clamp(14px,3.4vw,16px);line-height:1.45;
            font-weight:500;text-wrap:pretty}
            /* Whose it is, and no way in: a shared dream opens one dream and
               the rest of the board is not behind it. */
            .mark{font-size:13px;font-weight:500;letter-spacing:.06em;color:rgb(255 255 255/42%)}
            </style>
            </head>
            <body>
            <article class="card">
            {{frame}}
            <div class="body">
            <h1>{{title}}</h1>
            {{(line.Length > 0 ? $"<p class=\"line\">{line}</p>" : string.Empty)}}
            </div>
            </article>
            <small class="mark">Aspire</small>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Where this photograph is looked at, as inline CSS — the server's copy
    /// of `photoStyle` in `dreams/photos.ts`, because rule 16 says every
    /// surface that shows a photograph reads the point and the zoom, and this
    /// page is a surface like any other.
    ///
    /// A photograph nobody has moved gets no style at all: the middle and all
    /// of it is what `object-fit: cover` already does.
    /// </summary>
    private static string FocalStyle(DreamImage photo)
    {
        var x = Percent(photo.FocusX);
        var y = Percent(photo.FocusY);
        var zoom = double.IsFinite(photo.Zoom) ? Math.Max(1, photo.Zoom) : 1;

        var style = new StringBuilder();
        if (x != 50 || y != 50) style.Append($"object-position:{Number(x)}% {Number(y)}%;");
        if (zoom != 1)
        {
            style.Append($"transform:scale({Number(Round(zoom))});");
            style.Append($"transform-origin:{Number(x)}% {Number(y)}%;");
        }

        return style.ToString();
    }

    private static double Percent(double value) =>
        Round((double.IsFinite(value) ? Math.Min(1, Math.Max(0, value)) : 0.5) * 100);

    /// <summary>Two decimals is finer than any screen can show and shorter than a float.</summary>
    private static double Round(double value) => Math.Round(value * 100) / 100;

    /// <summary>A number in CSS, which has never heard of a Czech decimal comma.</summary>
    private static string Number(double value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);

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
