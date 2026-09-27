// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;

namespace BubbleBobbleSharpLib.Players;

internal readonly record struct PlayerCell(int Row, int Column, int FineX, int FineY)
{
    private const int Stride = 40;

    private const int LeftEdge = 0x14;
    private const int TopEdge = 0x15;

    private const int RowBias = 3;
    private const int ColumnBias = 1;

    internal static PlayerCell Of(byte x, byte y)
    {
        int column = (byte)(x - LeftEdge);
        int row = (byte)(y - TopEdge);

        return new((row >> 3) - RowBias, (column >> 3) - ColumnBias, column & 0x07, row & 0x07);
    }

    internal bool Solid(SolidMap map, int offset)
    {
        ArgumentNullException.ThrowIfNull(map);

        int column = Column + offset;
        int rows = column >= 0 ? column / Stride : ((column + 1) / Stride) - 1;

        return map[Row + rows, column - (rows * Stride)];
    }
}
