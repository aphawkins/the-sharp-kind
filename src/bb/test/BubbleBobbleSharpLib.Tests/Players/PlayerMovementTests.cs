// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// The walk, branch by branch, against the reference's own arithmetic.
//
// Every case starts the player at X $44 and Y $55, which PlayerCell puts on row 5, column 5 with
// both fine offsets zero. That matters: it is a position where the wall check runs, so a case that
// wants the check skipped has to move the player off it deliberately.
public sealed class PlayerMovementTests
{
    private const byte StartX = 0x44;
    private const byte StartY = 0x55;

    // A port byte reads $FF idle, and a bit goes clear while its direction is pushed.
    private const byte Idle = 0xFF;
    private const byte PushRight = Idle & unchecked((byte)~0x08);
    private const byte PushLeft = Idle & unchecked((byte)~0x04);
    private const byte PushUp = Idle & unchecked((byte)~0x01);

    // The cells the two probes reach from the starting position: $28 is one row down and none
    // across, $2B one row down and three across.
    private const int LeftCellRow = 6;
    private const int LeftCellColumn = 5;
    private const int RightCellRow = 6;
    private const int RightCellColumn = 8;

    // $22C3 sets $04 to 2 and falls into the tail that adds it to X.
    [Fact]
    public void WalksRightTwoPixelsWhenTheFrameAlreadyFacesRight()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);

        movement.Step(0, PushRight, Open());

        Assert.Equal(StartX + 2, players.X[0]);
    }

    // $226B sets $04 to $FE, which is minus two as a byte.
    [Fact]
    public void WalksLeftTwoPixelsWhenTheFrameAlreadyFacesLeft()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x04);

        movement.Step(0, PushLeft, Open());

        Assert.Equal(StartX - 2, players.X[0]);
    }

    // The turn. A player facing right and pushed left does not move on that push - $226B finds the
    // frame is not one of its own, writes its facing frame and returns. The step comes next push.
    [Fact]
    public void TurnsOnTheFirstPushAndWalksOnTheNext()
    {
        (PlayerTable players, EntityTable entities, PlayerMovement movement) = Walker(frame: 0x00);
        SolidMap map = Open();

        movement.Step(0, PushLeft, map);

        Assert.Equal(StartX, players.X[0]);
        Assert.Equal(0x04, entities.Frame[0]);

        movement.Step(0, PushLeft, map);

        Assert.Equal(StartX - 2, players.X[0]);
    }

    // $22A6 reads the cell the player is walking into and gives up if it is solid.
    [Fact]
    public void AWallStopsTheStep()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);

        movement.Step(0, PushRight, Blocked(RightCellRow, RightCellColumn));

        Assert.Equal(StartX, players.X[0]);
    }

    // Each mover looks at its own side, so a wall on the left does not stop a step to the right.
    [Fact]
    public void AWallOnTheOtherSideDoesNotStopTheStep()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);

        movement.Step(0, PushRight, Blocked(LeftCellRow, LeftCellColumn));

        Assert.Equal(StartX + 2, players.X[0]);
    }

    // $22A6 skips the check when $23 is set. A player part way into a cell has already been let into
    // it, so the wall in front is not tested again until they line up with the next one.
    [Fact]
    public void APlayerPartWayIntoACellWalksIntoTheWall()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);
        players.X[0] = StartX + 1;

        movement.Step(0, PushRight, Blocked(RightCellRow, RightCellColumn));

        Assert.Equal(StartX + 3, players.X[0]);
    }

    // And skips it below $2D, where there is no level to walk into.
    [Fact]
    public void AboveTheLevelThereIsNoWallToCheck()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);
        players.Y[0] = 0x20;

        movement.Step(0, PushRight, Solid());

        Assert.Equal(StartX + 2, players.X[0]);
    }

    // $220C reaches $222B with a jmp rather than a jsr, so a player pushing up never reaches the
    // movers at all - even with a direction held at the same time.
    [Fact]
    public void PushingUpSuppressesTheWalk()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);

        movement.Step(0, PushUp & PushRight, Open());

        Assert.Equal(StartX, players.X[0]);
    }

    // $2294. The sprite's bit 0 flips on every fourth frame, and the player keeps walking meanwhile.
    [Fact]
    public void TheWalkCycleStepsOnEveryFourthFrame()
    {
        (PlayerTable players, EntityTable entities, PlayerMovement movement) = Walker(frame: 0x00);
        SolidMap map = Open();

        for (int tick = 0; tick < 3; tick++)
        {
            movement.Step(0, PushRight, map);
            Assert.Equal(0x00, entities.Frame[0]);
        }

        movement.Step(0, PushRight, map);

        Assert.Equal(0x01, entities.Frame[0]);
        Assert.Equal(StartX + 8, players.X[0]);
    }

    // Frames 8 and 9 face right too, but reach the tail without passing through $2294, so they walk
    // without the cycle advancing.
    [Fact]
    public void TheFramesThatSkipTheAnimationStillWalk()
    {
        (PlayerTable players, EntityTable entities, PlayerMovement movement) = Walker(frame: 0x08);

        movement.Step(0, PushRight, Open());

        Assert.Equal(StartX + 2, players.X[0]);
        Assert.Equal(0x08, entities.Frame[0]);
        Assert.Equal(0x00, entities.AnimationTimer[0]);
    }

    // The turn is refused while the bubble timer says the player is in a bubble. $05C5 sets that
    // byte to $FF as a level starts, which is why the ordinary case is that they are not.
    [Fact]
    public void APlayerInABubbleWillNotTurn()
    {
        (PlayerTable players, EntityTable entities, PlayerMovement movement) = Walker(frame: 0x00);
        entities.BubbleTimer[0] = 0x00;

        movement.Step(0, PushLeft, Open());

        Assert.Equal(StartX, players.X[0]);
        Assert.Equal(0x00, entities.Frame[0]);
    }

    [Fact]
    public void AnIdlePortMovesNobody()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);

        movement.Step(0, Idle, Open());

        Assert.Equal(StartX, players.X[0]);
    }

    // The two players have their own bytes, and one walking does not move the other.
    [Fact]
    public void OnePlayerWalkingLeavesTheOtherWhereItWas()
    {
        (PlayerTable players, _, PlayerMovement movement) = Walker(frame: 0x00);
        players.X[1] = StartX;

        movement.Step(0, PushRight, Open());

        Assert.Equal(StartX + 2, players.X[0]);
        Assert.Equal(StartX, players.X[1]);
    }

    private static (PlayerTable Players, EntityTable Entities, PlayerMovement Movement) Walker(byte frame)
    {
        PlayerTable players = new();
        EntityTable entities = new();

        players.X[0] = StartX;
        players.Y[0] = StartY;
        entities.Frame[0] = frame;

        // What $05C5 leaves behind as a level starts: not in a bubble.
        entities.BubbleTimer[0] = 0xFF;
        entities.BubbleTimer[1] = 0xFF;

        return (players, entities, new(players, entities, TestBlow.Of(players, entities)));
    }

    private static SolidMap Open() => SolidMap.Build(Level(null, null));

    private static SolidMap Solid() => SolidMap.Build(Level(null, null, solid: true));

    private static SolidMap Blocked(int row, int column) => SolidMap.Build(Level(row, column));

    // A level that is open everywhere but the one cell asked for. Map row 0 is the ceiling, so the
    // bitmap's own rows start at map row 1.
    private static Level Level(int? row, int? column, bool solid = false)
    {
        List<string> bitmap = [.. Enumerable.Repeat(new string(solid ? '#' : '.', 32), 23)];

        if (row is int r && column is int c)
        {
            bitmap[r - 1] = $"{bitmap[r - 1][..c]}#{bitmap[r - 1][(c + 1)..]}";
        }

        return new() { Number = 1, Bitmap = bitmap };
    }
}
