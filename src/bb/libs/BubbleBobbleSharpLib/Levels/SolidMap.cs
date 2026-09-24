// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// $8500, sprites2-tables.s: which cells of the level a thing cannot pass through, and which way a
// current in them pushes a bubble.
//
// The reference keeps this as forty bytes a row - thirty-two columns of level, then eight bytes of
// something else - bit 7 solid and the low two bits a direction, 0 up, 1 right, 2 down, 3 left, that
// $0E00 reads for anything not answering to a player. Every collision test in the game is an indexed
// read off a pointer into it. This class keeps the reference's own byte, one per cell, minus the
// addressing: the port has the bit grid already, because init_level_renderer builds it from the same
// level bitmap the renderer draws, and a second copy addressed the C64's way would be the same truth
// written twice. What survives is the shape a probe sees - see PlayerCell, which turns a position
// into a row and a column here.
//
// Three rows above the map and three below it are copies of its edges. $8488, $84B0 and $84D8 start
// as rows of $80, but $3A6C in entity-spawn.s copies row 0 over all three as every level starts,
// and row 24 over $88E8, $8910 and $8938. The map's own addressing reaches them when a thing is
// near the top or the bottom, so a wrap opening goes on through them rather than stopping at a
// ceiling. Everything further off the map reads solid - nothing may leave the level by walking out
// of it - and direction 0.
//
// **Columns 32 to 39 are the row's tail.** The forty-byte row goes on past the level into eight
// bytes of an entity array, and a probe far enough past the edge reads one of them. IRowTails
// answers for those. The copies above and below the map take the tail of row 0 or row 24 as they
// are now, where the 6502 has the tail as it was when $3A6C copied it. No probe known reaches them.
internal sealed class SolidMap
{
    internal const int Columns = 32;
    internal const int Rows = 25;

    // $3A6C. How many rows past each edge are copies of it.
    private const int CopiedRows = 3;

    // Where a row's tail starts, and how long it is.
    private const int TailColumn = 32;
    private const int TailLength = 8;

    private const byte SolidBit = 0x80;
    private const byte DirectionMask = 0x03;

    // The level's own twenty-three rows sit between the ceiling on row 0 and the floor on row 24.
    private const int LevelRow = 1;

    // $E36E and $E371, the pair of bytes each half of the ceiling and the floor is built from. Index
    // 0 is the solid pair; 1 opens the gap in a left half and 2 the gap in a right half.
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

    // Which level this is, counted the way the game counts them, 1 to 100. It rides along because
    // one routine - $2569, the end of a jump - branches on the level number, and the map is what it
    // already has in its hand. The reference's own byte is one less than this: $E014 and everything
    // like it index from zero.
    internal int Number { get; }

    // A cell off the map is solid, which is the map's own arrangement rather than a guard: the
    // columns beside it are the wall the level is drawn inside. The three rows past each edge are
    // the edge again - see Copied.
    //
    // Without tails, a tail reads solid, which is what the renderer and a test with no entities want.
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

    // $E299 plus $E18B, decompress_level_data: init_level_renderer fills the hundred bytes the
    // renderer reads, and decompress_level_data lays a direction under every one of them before the
    // renderer ever runs - one per level, not per frame, so this is built once at ShowLevel and held
    // rather than rebuilt.
    //
    // zones is the level's own rectangles from zones.json, already resolved: the reference's mirror
    // is folded into a second rectangle by the export rather than replayed here, per Phase 2 and the
    // "direction field" item in Phase 6. An empty list is a level with no current anywhere a
    // rectangle does not reach, which is a real state - not every level carries one - not a caller
    // that forgot to pass zones.
    internal static SolidMap Build(Level level, IReadOnlyList<ZoneRect> zones, IRowTails? tails = null)
    {
        ArgumentNullException.ThrowIfNull(level);
        ArgumentNullException.ThrowIfNull(zones);

        byte[] cells = new byte[Rows * Columns];

        FillRowDefaults(cells, level);

        // $E2E9. wrap_openings is four bits - top left, top right, bottom left, bottom right - and a
        // set bit opens the gap a player wrapping round the screen goes through. A clear one leaves
        // that half of the row solid. Direction is untouched here: row 0 and row 24 already carry
        // bubbleCurrent uniformly, from FillRowDefaults, and the reference's own wall-marking pass
        // that sets this bit reads the drawn screen rather than the zone data, so the two never
        // fight over the same bit.
        Edge(cells, 0, (level.WrapOpenings & 0x01) != 0, (level.WrapOpenings & 0x02) != 0);
        Edge(cells, Rows - 1, (level.WrapOpenings & 0x04) != 0, (level.WrapOpenings & 0x08) != 0);

        // $E308. The bitmap is copied in a row at a time, and the first and last byte of each row
        // have their outer two bits set as they land: the level's leftmost and rightmost two columns
        // are wall whatever the bitmap says, which is what the sidebar decoration on both edges is
        // drawn over - see the playfield bullet, Phase 3.
        for (int row = 0; row < level.Bitmap.Count; row++)
        {
            string bits = level.Bitmap[row];

            for (int column = 0; column < Columns; column++)
            {
                bool solid = bits[column] == '#' || column < 2 || column >= Columns - 2;

                SetSolid(cells, row + LevelRow, column, solid);
            }
        }

        // $E1F3. A rectangle overwrites whichever cells it covers with one direction, the same
        // store every path through the reference's dispatcher makes - never the solid bit, which
        // zone data never carries.
        foreach (ZoneRect zone in zones)
        {
            Overlay(cells, zone);
        }

        return new(cells, level.Number, tails);
    }

    // $0E23. The direction a lone thing in this cell drifts, masked to two bits the way the
    // reference masks the byte it reads: 0 up, 1 right, 2 down, 3 left. The three rows past each
    // edge read the edge's direction, and further off the map reads 0.
    internal int Direction(int row, int column)
        => InBounds(Copied(row), column) ? _cells[(Copied(row) * Columns) + column] & DirectionMask : 0;

    private static bool InBounds(int row, int column)
        => row >= 0 && row < Rows && column >= 0 && column < Columns;

    // $3A6C: ldx #$27, then dex and bpl, so all forty bytes of row 0 go to each of the three rows
    // above, and all forty of row 24 to each of the three below. The copy is made once, after the
    // map is built, and this map does not change after that, so reading the edge row is the same.
    private static int Copied(int row) => row switch
    {
        >= -CopiedRows and < 0 => 0,
        >= Rows and < Rows + CopiedRows => Rows - 1,
        _ => row,
    };

    // $E1D1. Before any rectangle runs, every row is filled with one direction across its whole
    // width - the reference's own uniform pass, `AND #$03` against one byte per row. Row 0 and row
    // 24 read that byte at $8B03 and $8B63, which physics_flags' bubbleCurrent nibble is folded
    // into two bits at a time; every other row reads the last of the four bytes copied from the
    // level's own bitmap for that row - columns 30 and 31, reread as a direction rather than a wall.
    // Confirmed against a VICE capture of the transient buffer - see docs/bb-port-plan.md, Phase 6.
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

    // $E239. Clipped to the map rather than trusting a rectangle to stay inside it: every one in
    // zone-data.txt does, but nothing about the format guarantees it.
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

    // $E34E. Each half of the row is two bytes out of the table, chosen by that half's opening bit.
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
