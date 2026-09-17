// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// The decoration running down both edges of the level: one of fifty-nine
/// designs, four characters each, or none at all.
/// <para>
/// A design is four characters because that is what setup_level_screen copies
/// - thirty-two bytes, four characters of eight - into the four charset slots
/// the border tiles use. It is drawn as a two-by-two block, since draw_border
/// lays the first two characters side by side and the other two on the row
/// below, and repeats that block down the level's left and right edges.
/// </para>
/// </summary>
/// <param name="Design">
/// Which of the designs the level wears, counted from zero, or
/// <see cref="NoDesign"/>.
/// </param>
/// <param name="HeaderTile">
/// The level's own header tile, counted from zero, which stands in for a
/// design when the level has none: setup_level_screen copies that one
/// character into all four sidebar slots, so the block is that tile four
/// times over.
/// </param>
/// <param name="Colours">
/// The level's colour byte, as <see cref="PlayfieldModel.Colours"/> holds it.
/// The decoration is inside the level rather than beside it, so it is painted
/// out of the same two entries the level's own tiles are.
/// </param>
public sealed record SidebarModel(int Design, int HeaderTile, int Colours)
{
    /// <summary>Gets the design a level has when it has none of its own and repeats its header tile.</summary>
    public static int NoDesign => -1;

    /// <summary>Gets how many designs the sidebar sheet holds.</summary>
    public static int DesignCount => 59;

    /// <summary>Gets how many characters one design is made of.</summary>
    public static int CharactersPerDesign => 4;

    /// <summary>Gets a value indicating whether the level wears one of the designs.</summary>
    public bool HasDesign => Design != NoDesign;
}
