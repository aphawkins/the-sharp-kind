// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $E9B8 is arithmetic and nothing else, so every case here is the reference's own worked by hand:
// the two subtractions, the two divisions by eight, and the two biases the row table and the dec
// leave behind.
public sealed class PlayerCellTests
{
    // $24 is the leftmost X the movement code clamps to and $2D is a landed Y, so this is a player
    // standing in the corner of a level: column 2 of the map, on its third row.
    [Fact]
    public void PutsTheLeftmostLandedPositionOnTheFirstCellOfTheLevel()
    {
        PlayerCell cell = PlayerCell.Of(0x24, 0x2D);

        Assert.Equal(0, cell.Row);
        Assert.Equal(1, cell.Column);
        Assert.Equal(0, cell.FineX);
        Assert.Equal(0, cell.FineY);
    }

    // $F4 is the rightmost X the movement code clamps to, and $DD is where check_player_state puts a
    // player it has just respawned.
    [Fact]
    public void PutsTheRightmostRespawnPositionAtTheOtherEndOfTheMap()
    {
        PlayerCell cell = PlayerCell.Of(0xF4, 0xDD);

        Assert.Equal(22, cell.Row);
        Assert.Equal(27, cell.Column);
    }

    // The low three bits of each subtraction, which is how far past the cell the thing has travelled.
    // The reference keeps them in $23 and $24 and branches on them rather than on the position.
    [Fact]
    public void KeepsThePixelsThePositionIsPastItsCell()
    {
        PlayerCell cell = PlayerCell.Of(0x27, 0x30);

        Assert.Equal(3, cell.FineX);
        Assert.Equal(3, cell.FineY);
        Assert.Equal(1, cell.Column);
        Assert.Equal(0, cell.Row);
    }

    // Both subtractions are byte subtractions in the reference and wrap like them, so a position
    // above the top of the playfield lands low on the map rather than off it. Nothing depends on the
    // wrap being useful - it depends on it being the 6502's.
    [Fact]
    public void WrapsAPositionAboveThePlayfieldTheWayAByteSubtractionDoes()
    {
        PlayerCell cell = PlayerCell.Of(0x00, 0x10);

        Assert.Equal(28, cell.Column);
        Assert.Equal(4, cell.FineX);
        Assert.Equal(28, cell.Row);
        Assert.Equal(3, cell.FineY);
    }

    // A probe is the offset the reference writes, and forty bytes is a row: $51 is two rows down and
    // one column across, $28 the row below, $79 three rows down and one across. $44 and $55 put the
    // cell in the middle of the map, so that every probe lands inside the level's own bitmap.
    [Theory]
    [InlineData(0x00, 5, 5)]
    [InlineData(0x03, 5, 8)]
    [InlineData(0x28, 6, 5)]
    [InlineData(0x2B, 6, 8)]
    [InlineData(0x51, 7, 6)]
    [InlineData(0x53, 7, 8)]
    [InlineData(0x79, 8, 6)]
    [InlineData(0xA2, 9, 7)]
    public void ReadsTheCellTheOffsetNames(int offset, int row, int column)
    {
        PlayerCell cell = PlayerCell.Of(0x44, 0x55);

        Assert.Equal(5, cell.Row);
        Assert.Equal(5, cell.Column);
        Assert.True(cell.Solid(SolidMap.Build(Level(row, column)), offset));
    }

    // And nothing else: the same probe over a level whose one solid cell is the next one along reads
    // open, and the next offset along reads it.
    [Fact]
    public void ReadsNoCellButTheOneTheOffsetNames()
    {
        PlayerCell cell = PlayerCell.Of(0x44, 0x55);
        SolidMap map = SolidMap.Build(Level(7, 7));

        Assert.False(cell.Solid(map, 0x51));
        Assert.True(cell.Solid(map, 0x52));
    }

    // A level that is open everywhere but the one map cell asked for. Map row 0 is the ceiling, so
    // the bitmap's own rows start at map row 1.
    private static Level Level(int row, int column)
    {
        List<string> bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)];

        bitmap[row - 1] = $"{bitmap[row - 1][..column]}#{bitmap[row - 1][(column + 1)..]}";

        return new() { Number = 1, Bitmap = bitmap };
    }
}
