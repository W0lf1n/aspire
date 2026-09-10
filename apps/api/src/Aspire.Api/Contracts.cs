using Aspire.Domain;
using Aspire.Infrastructure.Media;

namespace Aspire.Api;

/// <summary>
/// The wire types, mirroring <c>packages/contracts/src/index.ts</c>.
///
/// Kept as records with the same field names and casing the client reads, so
/// the mirror is checked by the serializer rather than by reading two files
/// side by side. A field renamed on one side breaks the other's build.
/// </summary>
public sealed record HealthResponse(bool Ok, string Version);

public sealed record PairRequest(string Code, string DeviceName);

public sealed record PairResponse(string DeviceId, string Token);

/// <summary>
/// What the client sends to make or change a dream. Everything is optional
/// on the wire so a missing field earns a sentence, not a 400 from the binder.
/// </summary>
public sealed record DreamInput(
    string? Title,
    string? Why,
    DreamStatus? Status,
    DreamCategory? Category,
    int? TargetYear,
    string? Affirmation);

/// <summary>
/// One photograph, as URLs. <c>Ready</c> is false for the moment between the
/// upload and the resize; the client shows the sky and asks again.
/// </summary>
/// <summary>
/// What a device sends to be nudged in the morning (PLAN.md §3.6): the
/// browser's own subscription, and when it wants to hear from it.
/// </summary>
public sealed record NudgeInput(
    string? Endpoint,
    string? P256dh,
    string? Auth,
    NudgeMode? Mode,
    int? AtMinutes,
    int? UtcOffsetMinutes);

/// <summary>
/// A device's standing nudge, as it reads it back. No keys come out: the
/// server was told them and has no reason to say them again.
/// </summary>
public sealed record NudgeDto(NudgeMode Mode, int AtMinutes)
{
    public static NudgeDto From(PushSubscription subscription) =>
        new(subscription.Mode, subscription.AtMinutes);

    /// <summary>What a device that has never subscribed reads.</summary>
    public static NudgeDto None => new(NudgeMode.Off, PushSubscription.DefaultAtMinutes);
}

/// <summary>
/// The VAPID public key, which a browser needs before it can subscribe at
/// all. Empty when the server has not been given a key pair, and the client
/// then says so rather than offering a switch that cannot work.
/// </summary>
public sealed record PushKeyResponse(string PublicKey);

public sealed record DreamImageDto(
    Guid Id,
    int SortOrder,
    DreamImageKind Kind,
    int Width,
    int Height,
    bool Ready,
    string ThumbUrl,
    string ScreenUrl,
    string FullUrl)
{
    public static DreamImageDto From(DreamImage image) => new(
        image.Id,
        image.SortOrder,
        image.Kind,
        image.Width,
        image.Height,
        image.ProcessedAt is not null,
        MediaStore.UrlOf(image.DreamId, image.Id, "thumb"),
        MediaStore.UrlOf(image.DreamId, image.Id, "screen"),
        MediaStore.UrlOf(image.DreamId, image.Id, "full"));
}

public sealed record DreamDto(
    Guid Id,
    string Title,
    string Why,
    string Affirmation,
    DreamStatus Status,
    DreamCategory? Category,
    int SortOrder,
    int? TargetYear,
    int Likes,
    DateTimeOffset? AchievedAt,
    DateTimeOffset? LastShownAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DreamImageDto> Images)
{
    public static DreamDto From(Dream dream, IEnumerable<DreamImage> images) => new(
        dream.Id,
        dream.Title,
        dream.Why,
        dream.Affirmation,
        dream.Status,
        dream.Category,
        dream.SortOrder,
        dream.TargetYear,
        dream.Likes,
        dream.AchievedAt,
        dream.LastShownAt,
        dream.CreatedAt,
        images.Select(DreamImageDto.From).ToList());
}
