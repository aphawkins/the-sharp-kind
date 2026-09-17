// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Renditions;
using SharpKind.Abstraction.Renditions;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit tier, which for this game is the Commodore 64: a 320x200 canvas
/// of 40x25 8x8 character cells, a fixed 8x8 font and sixteen colours.
/// <para>
/// The machine the port is translated from, so its numbers are the
/// original's rather than a choice. It is named for the tier rather than for
/// the machine because the tier is what the engine knows and what a config
/// file can hold - see <see cref="Rendition"/>.
/// </para>
/// </summary>
public sealed class EightBitRendition : IBbRendition
{
    public Rendition Rendition => Rendition.EightBit;

    // 40 columns of 8 pixels. The playfield is the middle 32 of them, with
    // the sidebars either side.
    public int ScreenWidth => 320;

    // 25 rows of 8 pixels.
    public int ScreenHeight => 200;

    // 320x200 is a postage stamp on a modern display, so this rendition
    // magnifies as far as Elite's 8-bit tier does: quadrupled is 1280x800,
    // which every common display still shows whole.
    public IReadOnlyList<int> WindowScales => [1, 2, 3, 4];

    public int DefaultWindowScale => 4;
}
