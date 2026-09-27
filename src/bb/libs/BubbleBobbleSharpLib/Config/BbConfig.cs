// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Abstraction.Config;
using SharpKind.Abstraction.Renditions;

namespace BubbleBobbleSharpLib.Config;

internal sealed class BbConfig : ConfigSettings<BbConfigSettings>
{
    public BbConfig()
    {
        Engine.Rendition = RenditionNames.EightBit;
        Engine.FallbackRendition = RenditionNames.EightBit;
    }
}
