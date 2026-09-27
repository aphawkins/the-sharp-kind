// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;

namespace BubbleBobbleSharpLib.Levels;

internal static class Sidebars
{
    internal const int NoDesignFrom = 100;

    internal static SidebarModel Select(Level level)
    {
        ArgumentNullException.ThrowIfNull(level);

        int headerTile = level.Number - 1;

        return new(
            level.Sidebar >= NoDesignFrom ? SidebarModel.NoDesign : level.Sidebar,
            headerTile,
            level.Colours);
    }
}
