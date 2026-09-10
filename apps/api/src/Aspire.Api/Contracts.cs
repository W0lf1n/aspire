using Aspire.Domain;

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
public sealed record DreamInput(string? Title, string? Why, DreamStatus? Status, int? TargetYear);

public sealed record DreamDto(
    Guid Id,
    string Title,
    string Why,
    DreamStatus Status,
    int SortOrder,
    int? TargetYear,
    int Likes,
    DateTimeOffset? AchievedAt,
    DateTimeOffset CreatedAt)
{
    public static DreamDto From(Dream dream) => new(
        dream.Id,
        dream.Title,
        dream.Why,
        dream.Status,
        dream.SortOrder,
        dream.TargetYear,
        dream.Likes,
        dream.AchievedAt,
        dream.CreatedAt);
}
