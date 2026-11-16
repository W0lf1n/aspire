using System.Collections.Frozen;
using System.Text.Json;

namespace Aspire.Domain;

/// <summary>
/// An enum as it travels and as it is stored: its name in kebab-case, the
/// same word on the wire, in the database column and in `packages/contracts`
/// (`InProgress` is „in-progress“). The policy is the one the API's JSON
/// already uses, so the two cannot drift apart.
/// </summary>
public static class Wire<T> where T : struct, Enum
{
    private static readonly FrozenDictionary<T, string> Names = Enum.GetValues<T>()
        .ToFrozenDictionary(v => v, v => JsonNamingPolicy.KebabCaseLower.ConvertName(v.ToString()));

    private static readonly FrozenDictionary<string, T> Values =
        Names.ToFrozenDictionary(p => p.Value, p => p.Key);

    public static string Name(T value) =>
        Names.TryGetValue(value, out var name) ? name : throw new ArgumentOutOfRangeException(nameof(value), value, null);

    public static T Parse(string value) =>
        TryParse(value) ?? throw new ArgumentOutOfRangeException(nameof(value), value, null);

    /// <summary>The value a request named, or null when it named nothing valid — never a number.</summary>
    public static T? TryParse(string? value) =>
        value is not null && Values.TryGetValue(value, out var parsed) ? parsed : null;
}
