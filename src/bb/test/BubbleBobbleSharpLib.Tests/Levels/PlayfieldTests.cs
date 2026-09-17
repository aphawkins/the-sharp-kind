// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Levels;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The renderer's output is decided one cell at a time by what its neighbours hold already, so the
// checks here are of particular cells of particular levels, traced through the reference by hand,
// rather than of the shape of the whole.
[Trait("Level", "Integration")]
public sealed class PlayfieldTests
{
    // The screen codes the renderer can leave behind: a space, the level's own tile, or one of the
    // six the tile's edges and shadow are drawn with.
    private static readonly byte[] s_expected =
        [PlayfieldModel.Space, PlayfieldModel.LevelTile, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    [Fact]
    public void WritesNothingButTheCharactersTheRendererKnows()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            PlayfieldModel model = Playfield.Build(s_levels.Level(number));

            for (int row = 0; row < PlayfieldModel.Rows; row++)
            {
                for (int column = 0; column < PlayfieldModel.Columns; column++)
                {
                    byte character = model.Character(column, row);

                    Assert.True(
                        s_expected.Contains(character),
                        $"Level {number} has {character:X2} at column {column}, row {row}.");
                }
            }
        }
    }

    // $E308 sets the top two bits of every row of the bitmap as it lands, so the level's leftmost two
    // columns are wall whether the level drew them or not. It is what the sidebar decoration is drawn
    // over, and level 1 is one of the levels whose bitmap leaves column 0 open.
    [Fact]
    public void WallsOffTheLeftmostTwoColumnsOfEveryLevel()
    {
        for (int number = 1; number <= LevelStore.Count; number++)
        {
            PlayfieldModel model = Playfield.Build(s_levels.Level(number));

            for (int row = 1; row < PlayfieldModel.Rows - 1; row++)
            {
                Assert.Equal(PlayfieldModel.LevelTile, model.Character(0, row));
                Assert.Equal(PlayfieldModel.LevelTile, model.Character(1, row));
            }
        }
    }

    // Level 1 opens none of the four, so both the ceiling and the floor are solid across.
    [Fact]
    public void ClosesTheCeilingAndTheFloorWhenTheLevelOpensNeither()
    {
        PlayfieldModel model = Playfield.Build(s_levels.Level(1));

        Assert.Equal(0, s_levels.Level(1).WrapOpenings);

        for (int column = 0; column < PlayfieldModel.Columns; column++)
        {
            Assert.Equal(PlayfieldModel.LevelTile, model.Character(column, 0));
            Assert.Equal(PlayfieldModel.LevelTile, model.Character(column, PlayfieldModel.Rows - 1));
        }
    }

    // $E36E's second pair, $FF $87, is the left half of a ceiling with an opening in it: solid to
    // column 8, then four columns of nothing before the wall picks up again. Column 9 holds the edge
    // the tile at column 8 put there, and $E16F has turned it from the edge that stands over nothing
    // into the one that stands beside a tile, which only the top row gets.
    [Fact]
    public void OpensTheCeilingWhereTheLevelAsksForIt()
    {
        Level level = FirstLevelWith(0x01);
        PlayfieldModel model = Playfield.Build(level);

        Assert.Equal(PlayfieldModel.LevelTile, model.Character(8, 0));
        Assert.Equal(0x0E, model.Character(9, 0));
        Assert.Equal(PlayfieldModel.Space, model.Character(10, 0));
        Assert.Equal(PlayfieldModel.Space, model.Character(11, 0));
        Assert.Equal(PlayfieldModel.Space, model.Character(12, 0));
        Assert.Equal(PlayfieldModel.LevelTile, model.Character(13, 0));
    }

    // $E371's third pair, $E1 $FF, is the same for a right half: solid to column 18, four columns of
    // nothing, then the wall again from column 23.
    [Fact]
    public void OpensTheCeilingsRightHalfSeparately()
    {
        Level level = FirstLevelWith(0x02);
        PlayfieldModel model = Playfield.Build(level);

        Assert.Equal(PlayfieldModel.LevelTile, model.Character(18, 0));
        Assert.Equal(0x0E, model.Character(19, 0));
        Assert.Equal(PlayfieldModel.Space, model.Character(20, 0));
        Assert.Equal(PlayfieldModel.Space, model.Character(22, 0));
        Assert.Equal(PlayfieldModel.LevelTile, model.Character(23, 0));
    }

    // Level 1's ninth bitmap row is "..##...##################...##..", which the renderer draws on
    // row 9 with the two walled-off columns in front of it. The platform's own row is the tile four
    // times over and then the edge that stands over nothing.
    [Fact]
    public void EndsAPlatformWithTheEdgeThatStandsOverNothing()
    {
        PlayfieldModel model = Playfield.Build(s_levels.Level(1));

        Assert.Equal("..##...##################...##..", s_levels.Level(1).Bitmap[8]);

        for (int column = 0; column < 4; column++)
        {
            Assert.Equal(PlayfieldModel.LevelTile, model.Character(column, 9));
        }

        Assert.Equal(0x0C, model.Character(4, 9));
        Assert.Equal(PlayfieldModel.Space, model.Character(5, 9));
    }

    // And the row under it is the shadow: the character that starts one, the character that carries it
    // on, and the one that closes it off past the platform's end. Column 2 is none of those - the wall
    // beside it is drawn after the shadow is, and its edge is the one that stands over a shadow.
    [Fact]
    public void ShadowsThePlatformOnToTheRowBelow()
    {
        PlayfieldModel model = Playfield.Build(s_levels.Level(1));

        Assert.Equal(0x0F, model.Character(2, 10));
        Assert.Equal(0x0D, model.Character(3, 10));
        Assert.Equal(0x0B, model.Character(4, 10));
        Assert.Equal(0x0A, model.Character(7, 10));
    }

    // The level's own tile, counted from zero, as $E014 indexes it.
    [Fact]
    public void TakesTheLevelsOwnTile()
    {
        Assert.Equal(0, Playfield.Build(s_levels.Level(1)).Tile);
        Assert.Equal(99, Playfield.Build(s_levels.Level(100)).Tile);
    }

    private static Level FirstLevelWith(int opening) => Enumerable.Range(1, LevelStore.Count)
        .Select(s_levels.Level)
        .First(x => (x.WrapOpenings & opening) != 0);
}
