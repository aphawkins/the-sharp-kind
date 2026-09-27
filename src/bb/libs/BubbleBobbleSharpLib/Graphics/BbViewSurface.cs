// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Assets;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;

namespace BubbleBobbleSharpLib.Graphics;

/// <summary>What the game hands a view.</summary>
internal sealed class BbViewSurface : IViewSurface
{
    internal BbViewSurface(IGraphics graphics, ScreenLayout screen, IAssetLocator assetLocator)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(assetLocator);

        Graphics = graphics;

        Layout = new(screen.ScreenWidth, screen.ScreenHeight);

        Palette = PaletteReader.Read(assetLocator.PalettePath);
    }

    public IGraphics Graphics { get; }

    public BbViewLayout Layout { get; }

    public IPaletteCollection Palette { get; }
}
