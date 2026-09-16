// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Abstraction.Config;

namespace BubbleBobbleSharpLib.Config;

// The root of bubblebobble.sharp: shared engine settings plus the game's own.
internal sealed class BbConfig : ConfigSettings<BbConfigSettings>;
