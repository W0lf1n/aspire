namespace Aspire.Domain;

/// <summary>
/// One device's standing permission to be told about a dream in the morning
/// (PLAN.md §3.6), and when it wants to be.
///
/// It is per device, not per board: a phone and a tablet on the same board
/// are two subscriptions with two schedules, because it is the phone in a
/// pocket at seven that this is for. A device that unsubscribes leaves no
/// row — there is no such thing as a subscription that is off but kept.
/// </summary>
public sealed class PushSubscription
{
    /// <summary>Endpoints are URLs from the browser's own push service and can be long.</summary>
    public const int EndpointMaxLength = 512;
    public const int KeyMaxLength = 200;

    /// <summary>07:00, PLAN.md §3.6's default, as minutes past local midnight.</summary>
    public const int DefaultAtMinutes = 7 * 60;

    public Guid Id { get; set; }

    /// <summary>The board this device paired into; the nudge comes from its dreams.</summary>
    public required string BoardId { get; set; }

    /// <summary>The push service's URL for this device. Unique: it is the device.</summary>
    public required string Endpoint { get; set; }

    /// <summary>The subscription's public key, base64url, as the browser gave it.</summary>
    public required string P256dh { get; set; }

    /// <summary>The subscription's auth secret, base64url.</summary>
    public required string Auth { get; set; }

    public NudgeMode Mode { get; set; } = NudgeMode.Daily;

    /// <summary>When, as minutes past midnight where the device is.</summary>
    public int AtMinutes { get; set; } = DefaultAtMinutes;

    /// <summary>
    /// The device's offset from UTC in minutes, rather than a zone name.
    /// The API runs with <c>InvariantGlobalization</c>, so it has no IANA
    /// zone database to look a name up in; an offset needs none. It goes
    /// stale twice a year, and the device sends it again every time the app
    /// opens, so it is right again the first time anybody looks (D34).
    /// </summary>
    public int UtcOffsetMinutes { get; set; }

    /// <summary>
    /// The local date this device was last nudged, so a day gets one. A date
    /// rather than a timestamp: "has today had its nudge" is the question.
    /// </summary>
    public DateOnly? LastSentOn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Off, every day, or only on working days (PLAN.md §3.6).</summary>
public enum NudgeMode
{
    Off,
    Daily,
    Weekdays
}

/// <summary>
/// The mode as it travels and as it is stored, the way every other enum in
/// this project does it.
/// </summary>
public static class NudgeModeNames
{
    public static string ToWire(NudgeMode mode) => mode switch
    {
        NudgeMode.Off => "off",
        NudgeMode.Daily => "daily",
        NudgeMode.Weekdays => "weekdays",
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    public static NudgeMode Parse(string value) => value switch
    {
        "off" => NudgeMode.Off,
        "daily" => NudgeMode.Daily,
        "weekdays" => NudgeMode.Weekdays,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
