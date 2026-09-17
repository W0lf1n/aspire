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
    /// <summary>One to five times of day, in any order; the server tidies them (D72).</summary>
    IReadOnlyList<int>? Times,
    int? UtcOffsetMinutes);

/// <summary>
/// Where a device is now, sent every time the app opens (D34, D51).
///
/// Its own verb rather than a <c>PUT</c> of the whole subscription: a
/// <c>PUT</c> carrying no mode and no time writes „daily at seven“ over
/// whatever the row said, and opening the app must never move the hour the
/// person chose. This touches the offset and nothing else, and a device with
/// no subscription is a request that changes nothing.
/// </summary>
public sealed record NudgeOffsetInput(string? Endpoint, int? UtcOffsetMinutes);

/// <summary>
/// A device's standing nudge, as it reads it back. No keys come out: the
/// server was told them and has no reason to say them again.
/// </summary>
public sealed record NudgeDto(NudgeMode Mode, IReadOnlyList<int> Times)
{
    public static NudgeDto From(PushSubscription subscription) =>
        new(subscription.Mode, subscription.Times);

    /// <summary>What a device that has never subscribed reads.</summary>
    public static NudgeDto None => new(NudgeMode.Off, [PushSubscription.DefaultAtMinutes]);
}

/// <summary>
/// The VAPID public key, which a browser needs before it can subscribe at
/// all. Empty when the server has not been given a key pair, and the client
/// then says so rather than offering a switch that cannot work.
/// </summary>
public sealed record PushKeyResponse(string PublicKey);

/// <summary>
/// The board's lock-screen link, or none (D60).
///
/// The path rather than the whole URL: the client is served from this origin
/// and knows it for certain, while the server behind nginx would be reading
/// its own scheme out of a header somebody else can set. The screen puts the
/// two together, and adds this phone's canvas and offset.
/// </summary>
public sealed record LinkResponse(string? Path);

/// <summary>
/// Where a photograph is looked at, and how close (D54). Everything optional,
/// so a field left out keeps what the photograph already has.
/// </summary>
public sealed record FocalInput(
    double? FocusX,
    double? FocusY,
    double? Zoom,
    /// <summary>Filling the frame or whole inside it, and on what (D81).</summary>
    PhotoFit? Fit = null,
    PhotoMat? Mat = null);

public sealed record DreamImageDto(
    Guid Id,
    int SortOrder,
    DreamImageKind Kind,
    int Width,
    int Height,
    bool Ready,
    double FocusX,
    double FocusY,
    double Zoom,
    PhotoFit Fit,
    PhotoMat Mat,
    string ThumbUrl,
    string ScreenUrl,
    string LargeUrl)
{
    public static DreamImageDto From(DreamImage image) => new(
        image.Id,
        image.SortOrder,
        image.Kind,
        image.Width,
        image.Height,
        image.ProcessedAt is not null,
        image.FocusX,
        image.FocusY,
        image.Zoom,
        image.Fit,
        image.Mat,
        MediaStore.UrlOf(image.DreamId, image.Id, "thumb"),
        MediaStore.UrlOf(image.DreamId, image.Id, "screen"),
        // The 2048 file, named for what it is to a phone rather than for
        // what it is on disk: the rung a 2× or 3× screen reads on the reel
        // (D62). The file keeps its name, because a year of cached URLs is a
        // year of cached URLs (D23).
        MediaStore.UrlOf(image.DreamId, image.Id, "full"));
}

/// <summary>
/// The board as a whole, for the settings screen: how many photographs it
/// holds on the server and what they weigh, against the ceiling (D64).
/// </summary>
public sealed record BoardResponse(string Name, int Photographs, long Bytes, long BytesLimit);

/// <summary>
/// Starting over (D80): the phrase, typed out. The screen checks it on the
/// keystroke and the server checks it again.
/// </summary>
public sealed record ResetInput(string? Phrase);

/// <summary>How many dreams went, for the sentence that says so.</summary>
public sealed record ResetResponse(int Dreams);

/// <summary>
/// Teď, in one request: these dreams, in this order, and nothing else on it
/// (D53). An empty list is a Teď emptied on purpose.
/// </summary>
public sealed record FocusInput(IReadOnlyList<Guid>? DreamIds);

/// <summary>Which of the collage templates a dream's tile uses (D82).</summary>
public sealed record LayoutInput(int? Layout);

/// <summary>
/// Whether a dream's tile is the collage and then each photograph, or the
/// photographs alone (D87): „collage“ or „carousel“.
/// </summary>
public sealed record ViewInput(string? View);

/// <summary>
/// A dream's dreamt photographs, in the order they stand in: the first is the
/// cover every small surface shows, and the rest are the collage's cells in
/// reading order (D82).
/// </summary>
public sealed record ImageOrderInput(IReadOnlyList<Guid>? ImageIds);

/// <summary>
/// Which line of the Seznam a dream is moved to, counted from one (D79).
/// </summary>
public sealed record PlaceInput(int? Place);

public sealed record DreamDto(
    Guid Id,
    string Title,
    string Why,
    string Affirmation,
    DreamStatus Status,
    DreamCategory? Category,
    int SortOrder,
    int? FocusRank,
    int? TargetYear,
    int Layout,
    PhotoView PhotoView,
    int Likes,
    DateTimeOffset? AchievedAt,
    DateTimeOffset? LastShownAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
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
        dream.FocusRank,
        dream.TargetYear,
        dream.Layout,
        dream.PhotoView,
        dream.Likes,
        dream.AchievedAt,
        dream.LastShownAt,
        dream.CreatedAt,
        dream.UpdatedAt,
        images.Select(DreamImageDto.From).ToList());
}
