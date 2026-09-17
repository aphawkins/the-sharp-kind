// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Abstraction.Renditions;

/// <summary>
/// What each <see cref="Rendition"/> is called in a config file and in the
/// folder its assets sit in. Spelled out rather than taken from the enum:
/// two are hyphenated, and a name in a player's config has to survive a
/// rename of the member behind it.
/// </summary>
public static class RenditionNames
{
    // Every spelling that has named a rendition, including the enum member names an older build
    // wrote and the tier setting renditions replaced, so an old file keeps the player's choice.
    private static readonly Dictionary<string, Rendition> s_byName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["8-bit"] = Rendition.EightBit,
        ["16-bit"] = Rendition.SixteenBit,
        ["Modern"] = Rendition.Modern,
        ["8Bit"] = Rendition.EightBit,
        ["16Bit"] = Rendition.SixteenBit,
        ["EightBit"] = Rendition.EightBit,
        ["SixteenBit"] = Rendition.SixteenBit,
    };

    // A legacy name parses but is still rewritten to the name it now goes by.
    private static readonly HashSet<string> s_currentNames =
        new(["8-bit", "16-bit", "Modern"], StringComparer.Ordinal);

    /// <summary>
    /// Gets the name <see cref="Rendition.EightBit"/> goes by.
    /// </summary>
    public static string EightBit => "8-bit";

    /// <summary>
    /// Gets the name <see cref="Rendition.SixteenBit"/> goes by.
    /// </summary>
    public static string SixteenBit => "16-bit";

    /// <summary>
    /// Gets the name <see cref="Rendition.Modern"/> goes by.
    /// </summary>
    public static string Modern => "Modern";

    /// <summary>
    /// Gets every rendition's name, in the order the enum declares them.
    /// </summary>
    public static IReadOnlyList<string> All => [EightBit, SixteenBit, Modern];

    /// <summary>
    /// The name this rendition goes by.
    /// </summary>
    /// <param name="rendition">The rendition to name.</param>
    /// <returns>Its config and folder name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Not one of the three.</exception>
    public static string Of(Rendition rendition) => rendition switch
    {
        Rendition.EightBit => EightBit,
        Rendition.SixteenBit => SixteenBit,
        Rendition.Modern => Modern,
        _ => throw new ArgumentOutOfRangeException(nameof(rendition)),
    };

    /// <summary>
    /// The rendition a name refers to, legacy spellings included.
    /// </summary>
    /// <param name="name">The name from a config file.</param>
    /// <param name="rendition">The rendition it names.</param>
    /// <returns><see langword="true"/> if the name is one of the three.</returns>
    public static bool TryParse(string? name, out Rendition rendition)
    {
        if (name is null)
        {
            rendition = default;
            return false;
        }

        return s_byName.TryGetValue(name, out rendition);
    }

    /// <summary>
    /// Whether this is exactly the name a rendition goes by, not a legacy
    /// spelling of it. A config file holding anything else is repaired.
    /// </summary>
    /// <param name="name">The name to check.</param>
    /// <returns><see langword="true"/> if it is one of the three names.</returns>
    public static bool IsCurrentName(string? name) => name is not null && s_currentNames.Contains(name);
}
