// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit tier's character grid: the fixed 8x8 cell the machine this
/// stands in for addresses its screen in, laid over the viewport.
/// <para>
/// One definition for the tier, rather than one per view. The grid is a fact
/// about the screen, not about whatever is drawing on it, so the things that
/// are not views - the list styles, which are static and so could never
/// inherit it - ask here instead of each working it out again.
/// </para>
/// <para>
/// It is the 8-bit tier's alone. The 16-bit tier has a proportional font and
/// lays out in pixels, so it has no grid to state and is given none.
/// </para>
/// </summary>
internal static class EightBitGrid
{
    // Small and Large share the same fixed 8x8 bbc-micro cell, unlike the 16-bit proportional font -
    // the viewport is a whole 40x25 of them.
    internal const int CharacterWidth = 8;
    internal const int RowHeight = 8;

    /// <summary>
    /// The grid over a surface's viewport.
    /// </summary>
    /// <param name="surface">The surface whose viewport the grid covers.</param>
    /// <returns>The grid.</returns>
    internal static CharacterGrid Of(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        return new(CharacterWidth, RowHeight, new(surface.Layout.ViewportLeft, surface.Layout.ViewportTop));
    }
}
