// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The decoration down both edges of the level.</summary>
/// <param name="Design">The level's design, or <see cref="NoDesign"/>.</param>
/// <param name="HeaderTile">The level's header tile, used when it has no design.</param>
/// <param name="Colours">The level's colour byte.</param>
/// <param name="Number">The level's number, drawn in the top-left corner; 0 draws none.</param>
public sealed record SidebarModel(int Design, int HeaderTile, int Colours, int Number = 0)
{
    // $38E3: level 100 has no room for three digits, so it is drawn as two brackets (glyphs $0A and $0B).
    private const int LastLevel = 100;

    /// <summary>Gets the design of a level that has none.</summary>
    public static int NoDesign => -1;

    /// <summary>Gets how many designs the sidebar sheet holds.</summary>
    public static int DesignCount => 59;

    /// <summary>Gets how many characters one design is made of.</summary>
    public static int CharactersPerDesign => 4;

    /// <summary>Gets a value indicating whether the level wears one of the designs.</summary>
    public bool HasDesign => Design != NoDesign;

    /// <summary>Gets the two characters the level number is drawn as, or an empty string when none is drawn.</summary>
    /// <remarks>A zero tens digit is left blank ($38AD).</remarks>
    public string NumberText
    {
        get
        {
            if (Number <= 0)
            {
                return string.Empty;
            }

            if (Number == LastLevel)
            {
                return "[]";
            }

            char tens = Number < 10 ? ' ' : (char)('0' + (Number / 10));

            return $"{tens}{Number % 10}";
        }
    }
}
