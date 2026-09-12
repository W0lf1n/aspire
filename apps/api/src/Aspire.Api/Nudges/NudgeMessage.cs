using System.Text.Json;
using Aspire.Domain;
using Aspire.Infrastructure.Media;

namespace Aspire.Api.Nudges;

/// <summary>
/// What a nudge says, as the service worker reads it (PLAN.md §3.6): a
/// heading, a line under it, a photograph, and the id so tapping it opens the
/// dream it was about.
///
/// Deliberately small. A push service caps the payload, the whole of it is
/// encrypted into one record, and there is nothing here a notification cannot
/// show.
///
/// Two kinds, and never both in one morning (D57). The daily one is headed by
/// the dream's own title with its affirmation under it, which is the same line
/// the tile carries (D27). The anniversary is headed by how long ago, because
/// the occasion is the news and the dream's name belongs in the sentence that
/// follows it.
/// </summary>
public static class NudgeMessage
{
    public static string For(Dream dream, IEnumerable<DreamImage> images)
    {
        // The affirmation over the why, as the tile does it; and the title
        // alone when a dream has neither, which is still worth waking for.
        var line = dream.Affirmation.Length > 0 ? dream.Affirmation : dream.Why;

        return Payload(dream.Title, line.Length > 0 ? line : null, dream, images, DreamImageKind.Dreamt);
    }

    /// <summary>
    /// „Před rokem“ · „Splnil se ti sen „Dům u lesa“.“ — the two lines reading
    /// as one sentence, the way `formatAnniversary` says it on the board.
    ///
    /// The picture is the achieved one where there is one, because the proof
    /// is the photograph (D28) and this morning is about the proof.
    /// </summary>
    public static string ForAnniversary(Dream dream, int years, IEnumerable<DreamImage> images)
    {
        // Czech wants `rokem` for one and `lety` for every number above it.
        var heading = years == 1 ? "Před rokem" : $"Před {years} lety";

        return Payload(
            heading,
            $"Splnil se ti sen „{dream.Title}“.",
            dream,
            images,
            DreamImageKind.Achieved,
            DreamImageKind.Dreamt);
    }

    private static string Payload(
        string heading,
        string? line,
        Dream dream,
        IEnumerable<DreamImage> images,
        params DreamImageKind[] wanted)
    {
        // In the order asked for, and nothing outside it: a dream's two
        // photographs are never interchangeable (D28).
        var ready = images.Where(i => i.ProcessedAt is not null).ToList();
        var photo = wanted
            .Select(kind => ready.FirstOrDefault(i => i.Kind == kind))
            .FirstOrDefault(found => found is not null);

        return JsonSerializer.Serialize(new NudgePayload(
            heading,
            line,
            dream.Id,
            photo is null ? null : MediaStore.UrlOf(dream.Id, photo.Id, "screen")));
    }

    /// <summary>
    /// camelCase on the wire like everything else, because the thing that
    /// reads it is `service-worker.ts`.
    /// </summary>
    private sealed record NudgePayload(string title, string? line, Guid id, string? image);
}
