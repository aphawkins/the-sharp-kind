// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Tests.Players;

// A blow for the Phase 4 tests, which need one because $220C and $25F1 both reach it, and none of
// which is about blowing. The slots it fills go nowhere: every case in those files pushes a
// direction rather than fire, so nothing is ever put in them. BubbleBlowTests is where the blow
// itself is proved.
internal static class TestBlow
{
    internal static BubbleBlow Of(PlayerTable players, EntityTable entities)
        => new(players, entities, new ObjectTable());
}
