// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

//// JSON serializable

// One level's zone rectangles, as zones.json holds them. `Number` runs 0 to 99, the way
// zone-data.txt counts levels and SUBFLG does - one less than Level.Number - because this is an
// array position pulled out into a field for ZoneStore to check itself against, not a level asked
// for by the game's own counting.
public class LevelZones
{
    public int Number { get; set; }

    public IList<ZoneRect> Rects { get; init; } = [];
}
