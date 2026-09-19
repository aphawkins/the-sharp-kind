// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// The plain descent, branch by branch.
//
// There is no golden trace behind this one, and that is worth saying plainly. The captures that
// proved the walk and the arc never reached $EB48 through a player: every trap on $EB3F in VICE was
// an entity slot, because the player landed on a floor every time. So these cases are worked from
// the reference rather than from the game, and they are the weaker kind of evidence this port has.
//
// The positions are chosen so that every probe lands inside the level's own bitmap. A ground check
// only happens on a row boundary, which from $2D means a Y of five more than a multiple of eight.
public sealed class PlayerDescentTests
{
    private const byte StartX = 0x44;

    [Fact]
    public void FallsTwoPixelsAFrame()
    {
        (EntityTable entities, PlayerDescent descent) = Falling(0x50);

        descent.Step(0, PlayerCell.Of(StartX, 0x50), Open());

        Assert.Equal(0x52, entities.Y[0]);
        Assert.Equal(0x00, entities.GroundState[0]);
    }

    // $EB56. The wrap is an equality test, so it catches $F5 and nothing else.
    [Fact]
    public void WrapsFromTheBottomOfThePlayfieldToTheTop()
    {
        (EntityTable entities, PlayerDescent descent) = Falling(0xF3);

        descent.Step(0, PlayerCell.Of(StartX, 0xF3), Open());

        Assert.Equal(0x15, entities.Y[0]);
    }

    // And a thing that steps over $F5 rather than onto it keeps going.
    [Fact]
    public void DoesNotWrapSteppingPastTheBottom()
    {
        (EntityTable entities, PlayerDescent descent) = Falling(0xF4);

        descent.Step(0, PlayerCell.Of(StartX, 0xF4), Open());

        Assert.Equal(0xF6, entities.Y[0]);
    }

    // $EB7D. A solid cell on the row below, with the thing's own row clear, is the ground - and
    // $EB91 takes the ground byte back down to $FF.
    [Fact]
    public void MarksTheGroundWhenItFindsIt()
    {
        (EntityTable entities, PlayerDescent descent) = Falling(0x53);

        descent.Step(0, PlayerCell.Of(StartX, 0x53), Solid(7, 6));

        Assert.Equal(0x55, entities.Y[0]);
        Assert.Equal(0xFF, entities.GroundState[0]);
    }

    // $EB69. If the thing's own row is solid it is inside something, and the check gives up before
    // it ever looks below.
    [Fact]
    public void FindsNoGroundWhileInsideASolidCell()
    {
        (EntityTable entities, PlayerDescent descent) = Falling(0x53);

        descent.Step(0, PlayerCell.Of(StartX, 0x53), Solid((6, 6), (7, 6)));

        Assert.Equal(0x00, entities.GroundState[0]);
    }

    // $EB62. Off a row boundary there is no check at all, floor or no floor.
    [Fact]
    public void LooksForGroundOnlyOnARowBoundary()
    {
        (EntityTable entities, PlayerDescent descent) = Falling(0x51);

        descent.Step(0, PlayerCell.Of(StartX, 0x51), Solid(7, 6));

        Assert.Equal(0x53, entities.Y[0]);
        Assert.Equal(0x00, entities.GroundState[0]);
    }

    private static (EntityTable Entities, PlayerDescent Descent) Falling(byte y)
    {
        EntityTable entities = new();

        entities.X[0] = StartX;
        entities.Y[0] = y;

        return (entities, new(entities));
    }

    private static SolidMap Open() => SolidMap.Build(Level());

    private static SolidMap Solid(int row, int column) => SolidMap.Build(Level((row, column)));

    private static SolidMap Solid(params (int Row, int Column)[] cells) => SolidMap.Build(Level(cells));

    private static Level Level(params (int Row, int Column)[] cells)
    {
        List<string> bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)];

        foreach ((int row, int column) in cells)
        {
            bitmap[row - 1] = $"{bitmap[row - 1][..column]}#{bitmap[row - 1][(column + 1)..]}";
        }

        return new() { Number = 1, Bitmap = bitmap };
    }
}
