// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Renditions;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Abstraction.Renditions;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit tier, which for this game is the Commodore 64: 40x25 cells of
/// 8x8 pixels, a fixed 8x8 font and sixteen colours.
/// </summary>
public sealed class EightBitRendition : IBbRendition
{
    public Rendition Rendition => Rendition.EightBit;

    // Forty columns of eight pixels. The level takes the leftmost 32 of them, with its sidebar
    // decoration inside that rather than beside it - see BbViewLayout.
    public int ScreenWidth => 320;

    public int ScreenHeight => 200;

    // Quadrupled is 1280x800, which every common display still shows whole.
    public IReadOnlyList<int> WindowScales => [1, 2, 3, 4];

    public int DefaultWindowScale => 4;

    public IView<SidebarModel> CreateSidebarView(IViewSurface surface) => new SidebarView8Bit(surface);

    public IView<PlayfieldModel> CreatePlayfieldView(IViewSurface surface) => new PlayfieldView8Bit(surface);

    public IView<HudModel> CreateHudView(IViewSurface surface) => new HudView8Bit(surface);
}
