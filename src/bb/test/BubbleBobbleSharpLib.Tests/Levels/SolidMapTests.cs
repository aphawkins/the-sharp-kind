// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The map is what every collision test in the game reads, so what there is to prove is that a cell
// of it holds what $8500 holds: the level where the level is, wall where the reference keeps wall,
// and solid everywhere a probe can reach but a level cannot.
public sealed class SolidMapTests
{
    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    // $8488, $84B0 and $84D8 are three forty-byte rows of $80 above the map, and the map's own
    // addressing reaches them: a thing near the top of the playfield probes rows that are not the
    // level's. They read solid, and so does everything else off the map.
    [Theory]
    [InlineData(-3, 0)]
    [InlineData(-1, 0)]
    [InlineData(SolidMap.Rows, 0)]
    [InlineData(0, -1)]
    [InlineData(0, SolidMap.Columns)]
    public void ReadsSolidOffTheMap(int row, int column)
    {
        SolidMap map = SolidMap.Build(s_levels.Level(1));

        Assert.True(map[row, column]);
    }

    // $E308 sets the top two bits of every row of the bitmap as it lands, so the level's leftmost two
    // columns are wall whether the level drew them or not.
    [Fact]
    public void WallsOffTheLeftmostTwoColumnsOfEveryLevel()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            SolidMap map = SolidMap.Build(s_levels.Level(number));

            for (int row = 0; row < SolidMap.Rows; row++)
            {
                Assert.True(map[row, 0], $"Level {number}, row {row}, column 0.");
                Assert.True(map[row, 1], $"Level {number}, row {row}, column 1.");
            }
        }
    }

    // $E308 again: the level's own twenty-three rows land on rows 1 to 23, and a '#' is what a solid
    // cell is drawn as. Columns 0 and 1 are excluded because the wall above overrides them.
    [Fact]
    public void HoldsEveryLevelsBitmapOnTheRowsBelowTheCeiling()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            Level level = s_levels.Level(number);
            SolidMap map = SolidMap.Build(level);

            for (int row = 0; row < level.Bitmap.Count; row++)
            {
                for (int column = 2; column < SolidMap.Columns; column++)
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
        Level closed = Level([.. Enumerable.Repeat(new string('.', 32), 23)], 0x00);
        Level open = Level([.. Enumerable.Repeat(new string('.', 32), 23)], 0x0F);

        SolidMap shut = SolidMap.Build(closed);
        SolidMap ajar = SolidMap.Build(open);

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

    private static Level Level(IList<string> bitmap, int wrapOpenings)
        => new() { Number = 1, WrapOpenings = wrapOpenings, Bitmap = bitmap };
}
