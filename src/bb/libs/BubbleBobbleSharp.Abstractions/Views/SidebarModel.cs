// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>The decoration down both edges of the level.</summary>
/// <param name="Design">The level's design, or <see cref="NoDesign"/>.</param>
/// <param name="HeaderTile">The level's header tile, used when it has no design.</param>
/// <param name="Colours">The level's colour byte.</param>
public sealed record SidebarModel(int Design, int HeaderTile, int Colours)
{
    /// <summary>Gets the design of a level that has none.</summary>
    public static int NoDesign => -1;

    /// <summary>Gets how many designs the sidebar sheet holds.</summary>
    public static int DesignCount => 59;

    /// <summary>Gets how many characters one design is made of.</summary>
    public static int CharactersPerDesign => 4;

    /// <summary>Gets a value indicating whether the level wears one of the designs.</summary>
    public bool HasDesign => Design != NoDesign;
}
