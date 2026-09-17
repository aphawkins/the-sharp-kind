// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>
/// Everything a view is given to draw with: somewhere to draw, the metrics to
/// lay out against, and the rendition's colours. This is the whole of it - a
/// view sees no more of the game than these three members.
/// </summary>
public interface IViewSurface
{
    /// <summary>
    /// Gets the surface to draw on.
    /// </summary>
    public IGraphics Graphics { get; }

    /// <summary>
    /// Gets the rendition's screen metrics to lay out against.
    /// </summary>
    public BbViewLayout Layout { get; }

    /// <summary>
    /// Gets the rendition's palette.
    /// </summary>
    public IPaletteCollection Palette { get; }
}
