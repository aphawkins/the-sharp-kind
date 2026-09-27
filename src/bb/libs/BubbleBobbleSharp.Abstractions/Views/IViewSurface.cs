// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Abstractions.Views;

/// <summary>Everything a view is given to draw with.</summary>
public interface IViewSurface
{
    /// <summary>Gets the surface to draw on.</summary>
    public IGraphics Graphics { get; }

    /// <summary>Gets the rendition's screen metrics to lay out against.</summary>
    public BbViewLayout Layout { get; }

    /// <summary>Gets the rendition's palette.</summary>
    public IPaletteCollection Palette { get; }
}
