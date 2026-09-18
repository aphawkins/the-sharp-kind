// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Levels;

// $8500, sprites2-tables.s: which cells of the level a thing cannot pass through.
//
// The reference keeps this as forty bytes a row - thirty-two columns of level, then eight bytes of
// something else - and every collision test in the game is an indexed read off a pointer into it,
// with bit 7 set meaning solid. This class is that map, minus the addressing: the port has the bit
// grid already, because init_level_renderer builds it from the same level bitmap the renderer draws,
// and a second copy addressed the C64's way would be the same truth written twice. What survives is
// the shape a probe sees - see PlayerCell, which turns a position into a row and a column here.
//
// Three rows above the level are solid and always were: $8488, $84B0 and $84D8 are forty-byte rows
// of $80 that the map's own addressing reaches when a thing is near the top. Everything off the map
// reads solid here for the same reason - nothing may leave the level by walking out of it.
internal sealed class SolidMap
{
    internal const int Columns = 32;
    internal const int Rows = 25;

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

    private readonly bool[] _solid;

    private SolidMap(bool[] solid, int number)
    {
        _solid = solid;
        Number = number;
    }

    // Which level this is, counted the way the game counts them, 1 to 100. It rides along because
    // one routine - $2569, the end of a jump - branches on the level number, and the map is what it
    // already has in its hand. The reference's own byte is one less than this: $E014 and everything
    // like it index from zero.
    internal int Number { get; }

    // A cell off the map is solid, which is the map's own arrangement rather than a guard: the rows
    // above it hold $80 and the columns beside it are the wall the level is drawn inside.
    internal bool this[int row, int column]
        => row < 0
            || row >= Rows
            || column < 0
            || column >= Columns
            || _solid[(row * Columns) + column];

    // $E299. init_level_renderer fills the hundred bytes the renderer reads: thirty-two bits a row,
    // twenty-five rows. The level's own bitmap is the middle twenty-three of them.
    internal static SolidMap Build(Level level)
    {
        ArgumentNullException.ThrowIfNull(level);

        bool[] solid = new bool[Rows * Columns];

        // $E2E9. wrap_openings is four bits - top left, top right, bottom left, bottom right - and a
        // set bit opens the gap a player wrapping round the screen goes through. A clear one leaves
        // that half of the row solid.
        Edge(solid, 0, (level.WrapOpenings & 0x01) != 0, (level.WrapOpenings & 0x02) != 0);
        Edge(solid, Rows - 1, (level.WrapOpenings & 0x04) != 0, (level.WrapOpenings & 0x08) != 0);

        // $E308. The bitmap is copied in a row at a time, and the first byte of each row has its top
        // two bits set as it lands: the level's leftmost two columns are wall whatever the bitmap
        // says, which is what the sidebar decoration is drawn over.
        for (int row = 0; row < level.Bitmap.Count; row++)
        {
            string bits = level.Bitmap[row];

            for (int column = 0; column < Columns; column++)
            {
                solid[((row + LevelRow) * Columns) + column] = bits[column] == '#' || column < 2;
            }
        }

        return new(solid, level.Number);
    }

    // $E34E. Each half of the row is two bytes out of the table, chosen by that half's opening bit.
    private static void Edge(bool[] solid, int row, bool openLeft, bool openRight)
    {
        int[] left = s_edgeBytes[openLeft ? 1 : 0];
        int[] right = s_edgeBytes[openRight ? 2 : 0];

        int[] bytes = [left[0], left[1], right[0], right[1]];

        for (int index = 0; index < bytes.Length; index++)
        {
            for (int bit = 0; bit < 8; bit++)
            {
                solid[(row * Columns) + (index * 8) + bit] = (bytes[index] & (0x80 >> bit)) != 0;
            }
        }
    }
}
