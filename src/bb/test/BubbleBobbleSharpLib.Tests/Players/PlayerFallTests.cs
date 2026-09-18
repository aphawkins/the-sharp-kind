// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $257C, branch by branch.
//
// Like PlayerDescentTests these are worked from the reference rather than from a capture, and for
// the same reason: the runs that proved the walk and the arc never got a player off a ledge on
// level 1, whose platforms are wider than the arc is long. So they prove the translation against
// what is written, not against the running game, and that is the weaker kind of evidence.
//
// What they can do is pin the differences from $EB48, which is the thing most likely to go wrong:
// the two routines are close enough to be confused for each other and their constants are not the
// same. The height floor, the X snap and the steer in the tail each have a case here.
//
// The positions are chosen so that every probe lands inside the level's own bitmap. A ground check
// only happens on a row boundary, which from $2D means a Y of five more than a multiple of eight -
// and the Y tested is the one *after* the two pixels, not before.
public sealed class PlayerFallTests
{
    // An odd column, so that the snap on landing has something to move.
    private const byte StartX = 0x45;

    // Two pixels below a row boundary: a step from here lands on one.
    private const byte AlignedStart = 0x53;

    // An idle port with the right bit pulled low, which is what a pushed direction looks like.
    private const byte Right = Input.Idle & ~Input.Right;

    // $79 from the cell at StartX/AlignedStart, which is the row a floor has to be in.
    private const int FloorRow = 7;
    private const int FloorColumn = 6;

    [Fact]
    public void FallsTwoPixelsAFrame()
    {
        (PlayerTable players, EntityTable entities, PlayerFall fall) = Falling(0x50);

        fall.Step(0, Input.Idle, PlayerCell.Of(StartX, 0x50), Open());

        Assert.Equal(0x52, players.Y[0]);
        Assert.Equal(0x00, entities.GroundState[0]);
    }

    // $2586. The wrap is an equality test, exactly as $EB56 is, so it catches $F5 and nothing else -
    // and it is the whole of the frame, with no animation after it.
    [Fact]
    public void WrapsFromTheBottomOfThePlayfieldToTheTop()
    {
        (PlayerTable players, EntityTable entities, PlayerFall fall) = Falling(0xF3);

        fall.Step(0, Input.Idle, PlayerCell.Of(StartX, 0xF3), Open());

        Assert.Equal(0x15, players.Y[0]);
        Assert.Equal(0x00, entities.AnimationTimer[0]);
    }

    [Fact]
    public void DoesNotWrapSteppingPastTheBottom()
    {
        (PlayerTable players, _, PlayerFall fall) = Falling(0xF4);

        fall.Step(0, Input.Idle, PlayerCell.Of(StartX, 0xF4), Open());

        Assert.Equal(0xF6, players.Y[0]);
    }

    // $25AB and $25BF. A solid cell on the row below, with the player's own row clear, is the
    // ground, and the dec takes the ground byte back to $FF.
    [Fact]
    public void LandsOnAFloor()
    {
        (PlayerTable players, EntityTable entities, PlayerFall fall) = Falling(AlignedStart);

        fall.Step(0, Input.Idle, PlayerCell.Of(StartX, AlignedStart), Floor());

        Assert.Equal(0x55, players.Y[0]);
        Assert.Equal(0xFF, entities.GroundState[0]);
    }

    // $2597. A player whose own row is solid is inside something, and the check gives up before it
    // ever looks below.
    [Fact]
    public void FindsNoGroundWhileInsideASolidCell()
    {
        (PlayerTable _, EntityTable entities, PlayerFall fall) = Falling(AlignedStart);

        SolidMap map = Solid((6, FloorColumn), (FloorRow, FloorColumn));

        fall.Step(0, Input.Idle, PlayerCell.Of(StartX, AlignedStart), map);

        Assert.Equal(0x00, entities.GroundState[0]);
    }

    // $2591. Off a row boundary there is no check at all, floor or no floor.
    [Fact]
    public void LooksForGroundOnlyOnARowBoundary()
    {
        (PlayerTable players, EntityTable entities, PlayerFall fall) = Falling(0x51);

        fall.Step(0, Input.Idle, PlayerCell.Of(StartX, 0x51), Floor());

        Assert.Equal(0x53, players.Y[0]);
        Assert.Equal(0x00, entities.GroundState[0]);
    }

    // $25BF to $25E1. Which way a landing rounds X depends on which way the player faces, and the
    // sets are **not** $2519's - there, $04 to $09 round down and everything else rounds up. Here
    // $08 and $09 round up with the low frames, and $0A upwards rounds down with the middle ones.
    // A translation that shared PlayerLanding's rule would pass every other case here and fail
    // these four.
    [Theory]
    [InlineData(0x00, 0x46)]
    [InlineData(0x03, 0x46)]
    [InlineData(0x04, 0x44)]
    [InlineData(0x07, 0x44)]
    [InlineData(0x08, 0x46)]
    [InlineData(0x09, 0x46)]
    [InlineData(0x0A, 0x44)]
    [InlineData(0x0F, 0x44)]
    public void SquaresXUpInTheDirectionThePlayerFaces(byte frame, byte expected)
    {
        (PlayerTable players, EntityTable entities, PlayerFall fall) = Falling(AlignedStart);
        entities.Frame[0] = frame;

        fall.Step(0, Input.Idle, PlayerCell.Of(StartX, AlignedStart), Floor());

        Assert.Equal(expected, players.X[0]);
    }

    // $25E2 falling into $25F1. The steer runs on every second frame of a fall and not on the
    // others, which is the same cadence the drift's tail sets - and it is the only thing that moves
    // a falling player sideways.
    [Fact]
    public void SteersOnEverySecondFrame()
    {
        (PlayerTable players, _, PlayerFall fall) = Falling(0x50);
        fall.Step(0, Right, PlayerCell.Of(players.X[0], players.Y[0]), Open());
        Assert.Equal(StartX, players.X[0]);

        fall.Step(0, Right, PlayerCell.Of(players.X[0], players.Y[0]), Open());
        Assert.Equal(StartX + 1, players.X[0]);
    }

    // And a landing frame never reaches the tail, so a player who lands does not also steer.
    [Fact]
    public void DoesNotSteerOnTheFrameItLands()
    {
        (PlayerTable players, EntityTable entities, PlayerFall fall) = Falling(AlignedStart);
        entities.AnimationTimer[0] = 0x01;

        fall.Step(0, Right, PlayerCell.Of(StartX, AlignedStart), Floor());

        // $46 is the snap alone. A steer on top of it would have made it $47.
        Assert.Equal(0x46, players.X[0]);
        Assert.Equal(0x01, entities.AnimationTimer[0]);
    }

    private static (PlayerTable Players, EntityTable Entities, PlayerFall Fall) Falling(byte y)
    {
        PlayerTable players = new();
        EntityTable entities = new();

        players.X[0] = StartX;
        players.Y[0] = y;

        // What $05F5 leaves it at. The steer will not turn a player whose bubble timer is positive.
        entities.BubbleTimer[0] = 0xFF;

        return (players, entities, new(players, entities, new(players, entities, TestBlow.Of(players, entities))));
    }

    private static SolidMap Open() => SolidMap.Build(Level());

    private static SolidMap Floor() => Solid((FloorRow, FloorColumn));

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
