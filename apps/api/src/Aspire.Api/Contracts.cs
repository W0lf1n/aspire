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

public sealed record DreamDto(
    Guid Id,
    string Title,
    string Why,
    DreamStatus Status,
    int SortOrder,
    int? TargetYear,
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
        dream.AchievedAt,
        dream.CreatedAt);
}
