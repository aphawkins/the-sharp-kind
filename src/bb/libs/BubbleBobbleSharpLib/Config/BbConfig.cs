// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Abstraction.Config;
using SharpKind.Abstraction.Renditions;

namespace BubbleBobbleSharpLib.Config;

// The root of bubblebobble.sharp: shared engine settings plus the game's own.
internal sealed class BbConfig : ConfigSettings<BbConfigSettings>
{
    // The shared engine default is 16-bit, chosen for no reason particular to
    // any one game; this game has art for the 8-bit tier alone, which is the
    // C64 it was translated from. Set the same way Elite sets its own, so a
    // player who has never chosen gets the rendition that is actually there.
    // The fallback goes with it: this game ships 8-bit art alone, so
    // repairing an unusable value to the engine's 16-bit would leave it with
    // nothing to draw with.
    public BbConfig()
    {
        Engine.Rendition = RenditionNames.EightBit;
        Engine.FallbackRendition = RenditionNames.EightBit;
    }
}
