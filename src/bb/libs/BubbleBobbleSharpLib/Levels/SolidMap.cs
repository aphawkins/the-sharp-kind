// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

internal sealed class SolidMap
{
    internal const int Columns = 32;
    internal const int Rows = 25;

    private const int CopiedRows = 3;

    private const int TailColumn = 32;
    private const int TailLength = 8;

    private const byte SolidBit = 0x80;
    private const byte DirectionMask = 0x03;

    private const int LevelRow = 1;

    private static readonly int[][] s_edgeBytes =
    [
        [0xFF, 0xFF],
        [0xFF, 0x87],
        [0xE1, 0xFF],
    ];

    private readonly byte[] _cells;
    private readonly IRowTails? _tails;

    private SolidMap(byte[] cells, int number, IRowTails? tails)
    {
        _cells = cells;
        Number = number;
        _tails = tails;
    }

    internal int Number { get; }

    internal bool this[int row, int column]
    {
        get
        {
            int edge = Copied(row);

            return _tails != null && edge is >= 0 and < Rows && column is >= TailColumn and < TailColumn + TailLength
                ? _tails.Solid(edge, column - TailColumn)
                : !InBounds(edge, column) || (_cells[(edge * Columns) + column] & SolidBit) != 0;
        }
    }

    internal static SolidMap Build(Level level, IReadOnlyList<ZoneRect> zones, IRowTails? tails = null)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(zones);

        byte[] cells = new byte[Rows * Columns];

        FillRowDefaults(cells, level);

        Edge(cells, 0, (level.WrapOpenings & 0x01) != 0, (level.WrapOpenings & 0x02) != 0);
        Edge(cells, Rows - 1, (level.WrapOpenings & 0x04) != 0, (level.WrapOpenings & 0x08) != 0);

        for (int row = 0; row < level.Bitmap.Count; row++)
        {
            string bits = level.Bitmap[row];

            for (int column = 0; column < Columns; column++)
            {
                bool solid = bits[column] == '#' || column < 2 || column >= Columns - 2;

                SetSolid(cells, row + LevelRow, column, solid);
            }
        }

        foreach (ZoneRect zone in zones)
        {
            Overlay(cells, zone);
        }

        return new(cells, level.Number, tails);
    }

    internal int Direction(int row, int column)
        => InBounds(Copied(row), column) ? _cells[(Copied(row) * Columns) + column] & DirectionMask : 0;

    private static bool InBounds(int row, int column)
        => row >= 0 && row < Rows && column >= 0 && column < Columns;

    private static int Copied(int row) => row switch
    {
        >= -CopiedRows and < 0 => 0,
        >= Rows and < Rows + CopiedRows => Rows - 1,
        _ => row,
    };

    private static void FillRowDefaults(byte[] cells, Level level)
    {
        for (int row = 0; row < Rows; row++)
        {
            int direction = row switch
            {
                0 => level.BubbleCurrent & DirectionMask,
                Rows - 1 => (level.BubbleCurrent >> 2) & DirectionMask,
                _ => WallByteDirection(level, row),
            };

            for (int column = 0; column < Columns; column++)
            {
                cells[(row * Columns) + column] = (byte)direction;
            }
        }
    }

    private static int WallByteDirection(Level level, int row)
    {
        int bitmapRow = row - LevelRow;

        if (bitmapRow < 0 || bitmapRow >= level.Bitmap.Count)
        {
            return 0;
        }

        string bits = level.Bitmap[bitmapRow];

        return (bits[Columns - 2] == '#' ? 2 : 0) | (bits[Columns - 1] == '#' ? 1 : 0);
    }

    private static void Overlay(byte[] cells, ZoneRect zone)
    {
        int top = Math.Max(zone.Y, 0);
        int bottom = Math.Min(zone.Y + zone.Height, Rows);
        int left = Math.Max(zone.X, 0);
        int right = Math.Min(zone.X + zone.Width, Columns);

        for (int row = top; row < bottom; row++)
        {
            for (int column = left; column < right; column++)
            {
                SetDirection(cells, row, column, zone.Type);
            }
        }
    }

    private static void Edge(byte[] cells, int row, bool openLeft, bool openRight)
    {
        int[] left = s_edgeBytes[openLeft ? 1 : 0];
        int[] right = s_edgeBytes[openRight ? 2 : 0];

        int[] bytes = [left[0], left[1], right[0], right[1]];

        for (int index = 0; index < bytes.Length; index++)
        {
            for (int bit = 0; bit < 8; bit++)
            {
                SetSolid(cells, row, (index * 8) + bit, (bytes[index] & (0x80 >> bit)) != 0);
            }
        }
    }

    private static void SetSolid(byte[] cells, int row, int column, bool solid)
    {
        int index = (row * Columns) + column;

        cells[index] = solid ? (byte)(cells[index] | SolidBit) : (byte)(cells[index] & ~SolidBit);
    }

    private static void SetDirection(byte[] cells, int row, int column, int direction)
    {
        int index = (row * Columns) + column;

        cells[index] = (byte)((cells[index] & ~DirectionMask) | (direction & DirectionMask));
    }
}
