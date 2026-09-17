// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;

namespace SharpKind.Graphics.Tests;

// The grid is arithmetic over three numbers, which is the point of it being its own type: none of
// this needs a surface, a view or a backend to prove.
public sealed class CharacterGridTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 8)]
    [InlineData(39, 312)]
    public void PutsAColumnAtItsMultipleOfTheCellWidth(int column, float expected)
        => Assert.Equal(expected, new CharacterGrid(8, 8).Column(column));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 8)]
    [InlineData(24, 192)]
    public void PutsARowAtItsMultipleOfTheCellHeight(int row, float expected)
        => Assert.Equal(expected, new CharacterGrid(8, 8).Row(row));

    // A grid laid over part of a screen counts from where that part starts, not from the screen's corner.
    [Fact]
    public void CountsFromItsOwnOrigin()
    {
        CharacterGrid grid = new(8, 8, new(32, 16));

        Assert.Equal(32, grid.Column(0));
        Assert.Equal(40, grid.Column(1));
        Assert.Equal(16, grid.Row(0));
        Assert.Equal(new Vector2(48, 32), grid.Cell(2, 2));
    }

    // Cells need not be square: a multicolour character is half as wide as it is tall.
    [Fact]
    public void KeepsTheTwoAxesApart()
    {
        CharacterGrid grid = new(4, 8);

        Assert.Equal(12, grid.Column(3));
        Assert.Equal(24, grid.Row(3));
    }

    [Fact]
    public void CountsWholeCellsOnly()
    {
        CharacterGrid grid = new(8, 8);

        Assert.Equal(40, grid.ColumnsIn(320));
        Assert.Equal(40, grid.ColumnsIn(327));
        Assert.Equal(25, grid.RowsIn(200));
    }

    // Rounding up, never to the nearest: a label nudged forward clears what it names, and one nudged
    // back sits on top of it.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 8)]
    [InlineData(8, 8)]
    [InlineData(9, 16)]
    public void SnapsUpToTheNextCellBoundary(float x, float expected)
        => Assert.Equal(expected, new CharacterGrid(8, 8).SnapUp(new(x, 0)).X);

    [Fact]
    public void SnapsRelativeToTheOrigin()
        => Assert.Equal(new Vector2(40, 24), new CharacterGrid(8, 8, new(32, 16)).SnapUp(new(33, 17)));
}
