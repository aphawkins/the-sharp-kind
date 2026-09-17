// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;

namespace BubbleBobbleSharpLib.Levels;

// The sidebar half of setup_level_screen: it decides which four characters the level's border tiles
// are drawn from, and nothing else. The model it produces is what crosses to a view.
internal static class Sidebars
{
    // $E029. The index is compared against a hundred, not against the fifty-nine designs that exist:
    // anything from here up means the level has no design and repeats its own header tile instead.
    // Every level that takes that path carries $7F, but the reference tests the range rather than the
    // value, so this does too.
    internal const int NoDesignFrom = 100;

    // $E023. levels.txt spells the sidebar index and the symmetry flag as one byte, and the export
    // has already split them - convert-levels.py rejects an index with bit 7 set - so the `and #$7F`
    // the reference does first has nothing left to mask off here.
    internal static SidebarModel Select(Level level)
    {
        ArgumentNullException.ThrowIfNull(level);

        // $E014. The reference indexes its tables by a level counted from zero.
        int headerTile = level.Number - 1;

        return new(level.Sidebar >= NoDesignFrom ? SidebarModel.NoDesign : level.Sidebar, headerTile);
    }
}
