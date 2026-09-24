// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// The eight bytes that end each forty-byte row of $8500, past the level's thirty-two columns.
//
// They are entity arrays - $8520 on row 0, $8548 on row 1, and so on - so what a probe finds there
// is whatever those arrays hold at that moment. A probe reaches them when it looks far enough past
// the level's edge, and it reads bit 7 as solid, as it would a cell.
internal interface IRowTails
{
    // Bit 7 of the byte at column 32 plus index, on a row of the map from 0 to 24.
    public bool Solid(int row, int index);
}
