namespace Aspire.Domain;

/// <summary>
/// The nine areas a dream can belong to (PLAN.md §3.1), Yager's own set.
///
/// A fixed set rather than a table of the board's own (§8.3, D32): the point
/// of the set is that it is the method's, and it is the same argument that
/// keeps <see cref="DreamStatus"/> three words. A category a board could
/// rename would need a screen to rename it in, and this app has so far
/// refused to grow one.
///
/// A dream may have none. Adding one is three taps and a photograph, and a
/// question it must answer first is a question that stops it being written.
/// </summary>
public enum DreamCategory
{
    Home,
    Car,
    Travel,
    Family,
    Freedom,
    Giving,
    Business,
    Health,
    Fun
}

/// <summary>
/// The category as it travels and as it is stored: one lower-case word in
/// both places and in `packages/contracts`, the way the status and the image
/// kind already are.
/// </summary>
public static class DreamCategoryNames
{
    public static string ToWire(DreamCategory category) => category switch
    {
        DreamCategory.Home => "home",
        DreamCategory.Car => "car",
        DreamCategory.Travel => "travel",
        DreamCategory.Family => "family",
        DreamCategory.Freedom => "freedom",
        DreamCategory.Giving => "giving",
        DreamCategory.Business => "business",
        DreamCategory.Health => "health",
        DreamCategory.Fun => "fun",
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
    };

    public static DreamCategory Parse(string value) => value switch
    {
        "home" => DreamCategory.Home,
        "car" => DreamCategory.Car,
        "travel" => DreamCategory.Travel,
        "family" => DreamCategory.Family,
        "freedom" => DreamCategory.Freedom,
        "giving" => DreamCategory.Giving,
        "business" => DreamCategory.Business,
        "health" => DreamCategory.Health,
        "fun" => DreamCategory.Fun,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
    };
}
