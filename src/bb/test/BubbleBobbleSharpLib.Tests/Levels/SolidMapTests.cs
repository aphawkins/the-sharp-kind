// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The map is what every collision test in the game reads, so what there is to prove is that a cell
// of it holds what $8500 holds: the level where the level is, wall where the reference keeps wall,
// solid everywhere a probe can reach but a level cannot, and the direction a current in that cell
// carries.
public sealed class SolidMapTests
{
    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "zones.json"));

    // Past the three rows $3A6C copies at each edge, and past the columns, the map reads solid and
    // direction 0. Level 1's bubbleCurrent is 2, so a copied row would read 2 here.
    [Theory]
    [InlineData(-4, 0)]
    [InlineData(SolidMap.Rows + 3, 0)]
    [InlineData(0, -1)]
    [InlineData(0, SolidMap.Columns)]
    public void ReadsSolidOffTheMap(int row, int column)
    {
        SolidMap map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1));

        Assert.True(map[row, column]);
        Assert.Equal(0, map.Direction(row, column));
    }

    // $E308 sets the outer two bits of the first and last byte of every row of the bitmap as it
    // lands, so the level's leftmost and rightmost two columns are wall whether the level drew them
    // or not - the sidebar decoration on both edges is drawn over exactly this.
    [Fact]
    public void WallsOffTheOutermostTwoColumnsOfEveryLevel()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            SolidMap map = SolidMap.Build(s_levels.Level(number), s_zones.Zones(number));

            for (int row = 0; row < SolidMap.Rows; row++)
            {
                Assert.True(map[row, 0], $"Level {number}, row {row}, column 0.");
                Assert.True(map[row, 1], $"Level {number}, row {row}, column 1.");
                Assert.True(map[row, SolidMap.Columns - 2], $"Level {number}, row {row}, column 30.");
                Assert.True(map[row, SolidMap.Columns - 1], $"Level {number}, row {row}, column 31.");
            }
        }
    }

    // $E308 again: the level's own twenty-three rows land on rows 1 to 23, and a '#' is what a solid
    // cell is drawn as. The outermost two columns each side are excluded because the wall above
    // overrides them regardless of what the bitmap drew there.
    [Fact]
    public void HoldsEveryLevelsBitmapOnTheRowsBelowTheCeiling()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            Level level = s_levels.Level(number);
            SolidMap map = SolidMap.Build(level, s_zones.Zones(number));

            for (int row = 0; row < level.Bitmap.Count; row++)
            {
                for (int column = 2; column < SolidMap.Columns - 2; column++)
                {
                    Assert.Equal(level.Bitmap[row][column] == '#', map[row + 1, column]);
                }
            }
        }
    }

    // $E2E9. A clear opening bit leaves that half of the ceiling or the floor solid, and a set one
    // opens the gap a player wrapping round the screen goes through. The bytes are $FF for solid,
    // $87 for an open left half and $E1 for an open right half.
    [Fact]
    public void OpensTheCeilingAndTheFloorWhereTheLevelAsksAndNowhereElse()
    {
        Level closed = Level([.. Enumerable.Repeat(new string('.', 32), 23)], wrapOpenings: 0x00);
        Level open = Level([.. Enumerable.Repeat(new string('.', 32), 23)], wrapOpenings: 0x0F);

        SolidMap shut = SolidMap.Build(closed, []);
        SolidMap ajar = SolidMap.Build(open, []);

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            Assert.True(shut[0, column], $"Closed ceiling, column {column}.");
            Assert.True(shut[SolidMap.Rows - 1, column], $"Closed floor, column {column}.");
        }

        // $87 is 1000 0111 over columns 8 to 15, and $E1 is 1110 0001 over 16 to 23, so each
        // opening is the four cells the clear bits leave and the cells either side of it stay solid.
        int[] openings = [9, 10, 11, 12, 19, 20, 21, 22];

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            Assert.Equal(!openings.Contains(column), ajar[0, column]);
            Assert.Equal(!openings.Contains(column), ajar[SolidMap.Rows - 1, column]);
        }
    }

    // $E1D1. Row 0's own direction, before any rectangle touches it, is bubbleCurrent's low two
    // bits, uniformly across the whole row - $8B03 is where the reference reads it, ora'd in over
    // the wrap-opening table's own bytes. Confirmed against a VICE capture of level 1: bubbleCurrent
    // 2, row 0 reads $82 in every one of its thirty-two columns.
    [Fact]
    public void RowZeroDefaultsToBubbleCurrentsLowTwoBits()
    {
        Level level = Level([.. Enumerable.Repeat(new string('.', 32), 23)], wrapOpenings: 0x00, bubbleCurrent: 0x0D);

        SolidMap map = SolidMap.Build(level, []);

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            // 0x0D is 1101; the low two bits are 01.
            Assert.Equal(1, map.Direction(0, column));
        }
    }

    // $E34E, the second `lsr` pair: row 24 reads bubbleCurrent's high two bits at $8B63, the same
    // way row 0 reads its low two at $8B03.
    [Fact]
    public void RowTwentyFourDefaultsToBubbleCurrentsHighTwoBits()
    {
        Level level = Level([.. Enumerable.Repeat(new string('.', 32), 23)], wrapOpenings: 0x00, bubbleCurrent: 0x0D);

        SolidMap map = SolidMap.Build(level, []);

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            // 0x0D is 1101; the high two bits, after two more lsr, are 11.
            Assert.Equal(3, map.Direction(SolidMap.Rows - 1, column));
        }
    }

    // $E1D1 again, for the rows between: with no rectangle covering a cell, its direction is the
    // last two bits of that row's own last packed wall-bitmap byte - columns 30 and 31, reread as a
    // direction rather than a wall. Confirmed against the same VICE capture: level 1's rows 1-3 read
    // '#' at column 30 and '.' at 31, and all three read direction 2.
    [Fact]
    public void UncoveredRowsDefaultToTheLastTwoColumnsOfTheirOwnWallBitmap()
    {
        string wallAtThirty = new string('.', 30) + "#.";
        string openBoth = new('.', 32);

        Level level = Level([wallAtThirty, openBoth], wrapOpenings: 0x00);

        SolidMap map = SolidMap.Build(level, []);

        // Column 30 solid, 31 open: 2.
        Assert.Equal(2, map.Direction(1, 15));

        // Both open: 0.
        Assert.Equal(0, map.Direction(2, 15));
    }

    // The whole of it, together, against level 1's own row 1: $81 81 (rectangle, forced solid),
    // twelve more of the rectangle's own direction, four columns of the row's own default where the
    // rectangle does not reach, then the mirrored rectangle - direction 1 flipped to 3 - for the
    // rest of the row including the two forced-solid columns at the right edge. Read off VICE on
    // 2026-09-19: `81 81 01x13 02x4 03x12 83`. This is the byte-exact target the reference's own
    // level draws, so it stands as the golden case for everything above.
    [Fact]
    public void MatchesLevelOnesOwnRowOneByteForByte()
    {
        Level level = s_levels.Level(1);
        SolidMap map = SolidMap.Build(level, s_zones.Zones(1));

        byte[] expected =
        [
            0x81, 0x81, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x02, 0x02, 0x02, 0x02,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x83, 0x83,
        ];

        Assert.Equal(SolidMap.Columns, expected.Length);

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            bool solid = (expected[column] & 0x80) != 0;
            int direction = expected[column] & 0x03;

            Assert.Equal(solid, map[1, column]);
            Assert.Equal(direction, map.Direction(1, column));
        }
    }

    // $3A6C copies all of row 0 into $8488, $84B0 and $84D8, the three rows above the map, and all
    // of row 24 into the three below. So a wrap opening goes on through them, and they carry the
    // edge's direction. The tables in sprites2-tables.s start them as $80, but that is before any
    // level has started.
    [Theory]
    [InlineData(-3, 0)]
    [InlineData(-1, 0)]
    [InlineData(SolidMap.Rows, SolidMap.Rows - 1)]
    [InlineData(SolidMap.Rows + 2, SolidMap.Rows - 1)]
    public void CopiesTheEdgeRowsThreeRowsOutwards(int row, int edge)
    {
        Level level = Level([.. Enumerable.Repeat(new string('.', 32), 23)], wrapOpenings: 0x0F, bubbleCurrent: 0x0D);

        SolidMap map = SolidMap.Build(level, []);

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            Assert.Equal(map[edge, column], map[row, column]);
            Assert.Equal(map.Direction(edge, column), map.Direction(row, column));
        }

        Assert.False(map[row, 9]);
        Assert.NotEqual(0, map.Direction(row, 9));
    }

    // Read out of VICE on level 1, stopped at $3A89 straight after the copy, on 2026-09-23. $8488,
    // $84B0 and $84D8 all read $82 in all thirty-two columns, which is row 0 and not the $80 the
    // tables start them as. $88E8, $8910 and $8938 all read $80, which is row 24.
    [Fact]
    public void ReadsTheCopiedRowsTheWayTheGameHasThemOnLevelOne()
    {
        SolidMap map = SolidMap.Build(s_levels.Level(1), s_zones.Zones(1));

        for (int column = 0; column < SolidMap.Columns; column++)
        {
            for (int row = -3; row < 0; row++)
            {
                Assert.True(map[row, column]);
                Assert.Equal(2, map.Direction(row, column));
            }

            for (int row = SolidMap.Rows; row < SolidMap.Rows + 3; row++)
            {
                Assert.True(map[row, column]);
                Assert.Equal(0, map.Direction(row, column));
            }
        }
    }

    private static Level Level(IList<string> bitmap, int wrapOpenings, int bubbleCurrent = 0)
        => new() { Number = 1, WrapOpenings = wrapOpenings, BubbleCurrent = bubbleCurrent, Bitmap = bitmap };
}
