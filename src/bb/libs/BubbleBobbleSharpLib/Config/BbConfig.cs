// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Abstraction.Config;
using SharpKind.Abstraction.Renditions;

namespace BubbleBobbleSharpLib.Config;

// The root of bubblebobble.sharp: shared engine settings plus the game's own.
internal sealed class BbConfig : ConfigSettings<BbConfigSettings>
{
    // The engine defaults to 16-bit; this game has art for the 8-bit tier alone. The fallback
    // goes with it, or a repair would leave the game with nothing to draw with.
    public BbConfig()
    {
        Engine.Rendition = RenditionNames.EightBit;
        Engine.FallbackRendition = RenditionNames.EightBit;
    }
}
