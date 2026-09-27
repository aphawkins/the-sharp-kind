// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

public class LevelZones
{
    public int Number { get; set; }

    public IList<ZoneRect> Rects { get; init; } = [];
}
