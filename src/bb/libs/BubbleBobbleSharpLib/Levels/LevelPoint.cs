// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

//// JSON serializable

// A tile position in the 32x23 playfield, as levels.txt writes one.
public class LevelPoint
{
    public int X { get; set; }

    public int Y { get; set; }
}
