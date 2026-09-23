// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

//// JSON serializable

// One rectangle out of zone-data.txt: a block of the collision map that carries a current, rather
// than whatever its row would otherwise default to. `Type` is the two-bit direction $E23 masks a
// cell's byte down to - 0 up, 1 right, 2 down, 3 left - the same field SolidMap keeps alongside
// solidity. The export has already resolved the reference's own mirroring into a second rectangle
// where one applies, so this side reads rectangles and nothing else - see docs/bb-port-plan.md,
// Phase 2 and the "direction field" item in Phase 6.
public class ZoneRect
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int Type { get; set; }
}
