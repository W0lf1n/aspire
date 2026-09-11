using System.Text.Json;
using Aspire.Domain;
using Aspire.Infrastructure.Media;

namespace Aspire.Api.Nudges;

/// <summary>
/// What a nudge says, as the service worker reads it (PLAN.md §3.6): one
/// dream's image and title, and the id so tapping it opens that dream.
///
/// Deliberately small. A push service caps the payload, the whole of it is
/// encrypted into one record, and there is nothing here a notification
/// cannot show — the body is the dream's own affirmation or why, which is
/// the same line the tile carries (D27).
/// </summary>
public static class NudgeMessage
{
    /// <summary>The title of every nudge: it is the app saying good morning.</summary>
    public const string Title = "Dnešní sen";

    public static string For(Dream dream, IEnumerable<DreamImage> images)
    {
        var photo = images.FirstOrDefault(i => i.ProcessedAt is not null && i.Kind == DreamImageKind.Dreamt);

        // The affirmation over the why, as the tile does it; and the title
        // alone when a dream has neither, which is still worth waking for.
        var line = dream.Affirmation.Length > 0 ? dream.Affirmation : dream.Why;

        return JsonSerializer.Serialize(new NudgePayload(
            Title,
            dream.Title,
            line.Length > 0 ? line : null,
            dream.Id,
            photo is null ? null : MediaStore.UrlOf(dream.Id, photo.Id, "screen")));
    }

    /// <summary>
    /// camelCase on the wire like everything else, because the thing that
    /// reads it is `service-worker.ts`.
    /// </summary>
    private sealed record NudgePayload(string title, string dream, string? line, Guid id, string? image);
}
