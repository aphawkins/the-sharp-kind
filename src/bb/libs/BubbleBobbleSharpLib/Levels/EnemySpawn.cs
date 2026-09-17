// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

//// JSON serializable

// One entry of a level's enemy list. The flags byte levels.txt spells as
// $0-$1F is exported already split into the parts the spawn code reads.
public class EnemySpawn
{
    public int X { get; set; }

    public int Y { get; set; }

    public int Type { get; set; }

    public int Delay { get; set; }

    public bool FaceLeft { get; set; }

    public bool MoveLeft { get; set; }

    // The low three bits the 6502 keeps in the spawn record's first byte; bit 0 means move right.
    public int Byte1Low { get; set; }
}
