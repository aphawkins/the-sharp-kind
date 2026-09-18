// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

// $E9B8, convert_entity_pos_to_screen: where a thing's two position bytes put it on the collision
// map, and how far past that cell it has travelled.
//
// The 6502 leaves the answer as a pointer. It divides X by eight for a column and Y by eight for a
// row, looks the row up in a table of forty-byte strides, and keeps the pointer in $11/$12 so that
// every later test is one indexed read - $51 for the cell two rows down and one across, $28 for the
// cell straight below, and so on. The constants are worth keeping, because they are what the
// reference is written in: Solid takes one of them and does the same arithmetic the 6502's index
// register does.
//
// Two biases come out of the pointer rather than out of the position. The row table starts at $FF88
// rather than at zero, which is three forty-byte rows below the map, and the column has a dec
// against it. Both are folded in here, so that a probe is the offset the reference names and
// nothing else.
internal readonly record struct PlayerCell(int Row, int Column, int FineX, int FineY)
{
    // Forty bytes a row, which is the C64 screen's width rather than the level's: the eight columns
    // past the level's thirty-two hold other things, and no probe in the reference reaches them.
    private const int Stride = 40;

    // Where the playfield starts, in each axis. $E9B8 subtracts one from X and the other from Y
    // before dividing, so the first cell of the level is the first cell of the map.
    private const int LeftEdge = 0x14;
    private const int TopEdge = 0x15;

    // $AC03 starts the row table at $FF88, three rows below $8500, and $11 is decremented once after
    // the column lands in it.
    private const int RowBias = 3;
    private const int ColumnBias = 1;

    // $E9B8. The subtractions are byte subtractions in the reference and wrap like them, so a thing
    // above the top of the playfield lands high on the map rather than off it.
    internal static PlayerCell Of(byte x, byte y)
    {
        int column = (byte)(x - LeftEdge);
        int row = (byte)(y - TopEdge);

        return new((row >> 3) - RowBias, (column >> 3) - ColumnBias, column & 0x07, row & 0x07);
    }

    // One indexed read off the pointer, by the offset the reference writes. $51 is two rows down and
    // one column across, because forty is a row; $28 is the row below; $00 is the cell itself.
    internal bool Solid(SolidMap map, int offset)
    {
        ArgumentNullException.ThrowIfNull(map);

        return map[Row + (offset / Stride), Column + (offset % Stride)];
    }
}
