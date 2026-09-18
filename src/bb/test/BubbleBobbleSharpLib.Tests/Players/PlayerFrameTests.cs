// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $1CBD, $1E6C and $2162: which of Phase 4's routines owns a frame.
//
// Nothing here re-proves the walk, the arc or the fall - each has its own tests, and the walk and
// the arc have golden traces behind them. What these cases prove is the chain of tests that picks
// between them, because that is all this class is: four questions asked in order, and a wrong
// answer to any of them sends the frame to the wrong routine.
//
// The player stands at $44/$55, which is a column boundary on a row boundary with a floor under it.
// That is the position the ground check at $21CD is written for, and every case below starts from it
// so that a test that changes the outcome has changed one thing.
public sealed class PlayerFrameTests
{
    private const byte StandX = 0x44;
    private const byte StandY = 0x55;

    // $51 from the cell at StandX/StandY: the floor a standing player is standing on.
    private const int FloorRow = 7;
    private const int FloorColumn = 6;

    // An idle port with one bit pulled low, which is what a pushed direction looks like.
    private const byte Right = Input.Idle & ~Input.Right;

    private const byte Up = Input.Idle & ~Input.Up;

    // $1CDB. A zero state byte is an empty slot, and the loop never calls $1E6C for it. Player two
    // is in exactly that state for the whole of a one-player game.
    [Fact]
    public void LeavesAnEmptySlotAlone()
    {
        (PlayerTable players, _, PlayerFrame frame) = Standing();
        players.State[1] = 0x00;
        players.X[1] = 0xEC;

        frame.Step([Right, Right], Floor());

        Assert.Equal(0xEC, players.X[1]);
    }

    // And the same slot driven for itself, so that the skip is the state byte rather than the index.
    [Fact]
    public void LeavesASlotInAnUntranslatedStateAlone()
    {
        (PlayerTable players, _, PlayerFrame frame) = Standing();
        players.State[0] = 0x04;

        frame.Step(0, Right, Floor());

        Assert.Equal(StandX, players.X[0]);
    }

    // $21EB into $220C. Standing on a floor with the stick pushed, the walk owns the frame - two
    // pixels, which is the walk's step and not the steer's one.
    [Fact]
    public void WalksAPlayerStandingOnAFloor()
    {
        (PlayerTable players, _, PlayerFrame frame) = Standing();

        frame.Step(0, Right, Floor());

        Assert.Equal(StandX + 2, players.X[0]);
    }

    // $21CD into $21E5. No floor, and on a row boundary, so the ground byte comes out of $FF and
    // the fall runs on the same frame rather than on the next one.
    [Fact]
    public void StartsAFallWhenTheFloorIsGone()
    {
        (PlayerTable players, EntityTable entities, PlayerFrame frame) = Standing();

        frame.Step(0, Input.Idle, Open());

        Assert.Equal(0x00, entities.GroundState[0]);
        Assert.Equal(StandY + 2, players.Y[0]);
    }

    // $21CD's first test. Off a row boundary the ground is not looked for at all, so a player part
    // way down a row keeps walking over a gap - they were standing a moment ago.
    [Fact]
    public void SkipsTheGroundCheckOffARowBoundary()
    {
        (PlayerTable players, EntityTable entities, PlayerFrame frame) = Standing();
        players.Y[0] = StandY + 1;

        frame.Step(0, Right, Open());

        Assert.Equal(0xFF, entities.GroundState[0]);
        Assert.Equal(StandX + 2, players.X[0]);
    }

    // $21C5. Already off the ground with no arc running, and the frame belongs to $257C whatever
    // the stick is doing.
    [Fact]
    public void FallsWhileTheGroundByteSaysThereIsNoGround()
    {
        (PlayerTable players, EntityTable entities, PlayerFrame frame) = Standing();
        entities.GroundState[0] = 0x00;

        frame.Step(0, Right, Floor());

        Assert.Equal(StandY + 2, players.Y[0]);
        Assert.Equal(StandX, players.X[0]);
    }

    // $21BD. A rise counter out of its idle means an arc is running, and the arc wins over both the
    // ground check and the stick - this is the case that keeps a player rising through a ceiling of
    // their own floor.
    [Fact]
    public void RunsTheArcWhileTheRiseCounterIsSet()
    {
        (PlayerTable players, EntityTable entities, PlayerFrame frame) = Standing();
        entities.RiseCounter[0] = 0x0F;

        frame.Step(0, Input.Idle, Floor());

        // $ACCD's last entry is four, and the rise subtracts it.
        Assert.Equal(StandY - 4, players.Y[0]);
        Assert.Equal(0x0E, entities.RiseCounter[0]);
    }

    // The trigger and the arc, through the chain rather than through $222B directly: up starts the
    // jump on one frame and the next frame is already rising.
    [Fact]
    public void JumpsOnUpAndRisesOnTheFrameAfter()
    {
        (PlayerTable players, EntityTable entities, PlayerFrame frame) = Standing();

        frame.Step(0, Up, Floor());

        Assert.Equal(0x0F, entities.RiseCounter[0]);
        Assert.Equal(StandY, players.Y[0]);

        frame.Step(0, Up, Floor());

        Assert.Equal(StandY - 4, players.Y[0]);
    }

    // $21F3 to $2209. Nothing pushed, and the player breathes on a period of seven - a third of the
    // walk's pace, and the reason a standing player is not a still image.
    [Fact]
    public void AnimatesAnIdlePlayerEverySeventhFrame()
    {
        (PlayerTable players, EntityTable entities, PlayerFrame frame) = Standing();
        SolidMap floor = Floor();

        for (int tick = 0; tick < 6; tick++)
        {
            frame.Step(0, Input.Idle, floor);
        }

        Assert.Equal(0x00, entities.Frame[0]);
        Assert.Equal(StandX, players.X[0]);

        frame.Step(0, Input.Idle, floor);

        Assert.Equal(0x01, entities.Frame[0]);
        Assert.Equal(0x00, entities.AnimationTimer[0]);
    }

    // $21F3's own test. In a bubble the stick reaches the movers even when it is idle, so the idle
    // animation never runs - which is what tells the two apart.
    [Fact]
    public void DoesNotAnimateAPlayerInABubble()
    {
        (_, EntityTable entities, PlayerFrame frame) = Standing();
        entities.BubbleTimer[0] = 0x10;
        SolidMap floor = Floor();

        for (int tick = 0; tick < 7; tick++)
        {
            frame.Step(0, Input.Idle, floor);
        }

        Assert.Equal(0x00, entities.Frame[0]);
    }

    // $1CBD's loop reads both ports and walks both slots, and one player's stick never reaches the
    // other - which is the property Input.Read is built on, carried through the driver.
    [Fact]
    public void DrivesTheTwoPlayersFromTheirOwnSticks()
    {
        (PlayerTable players, _, PlayerFrame frame) = Standing();
        players.State[1] = PlayerFrame.PlayingState;
        players.X[1] = StandX;
        players.Y[1] = StandY;

        frame.Step([Right, Input.Idle], Floor());

        Assert.Equal(StandX + 2, players.X[0]);
        Assert.Equal(StandX, players.X[1]);
    }

    // A player standing on a floor with nothing pushed stays exactly where they are. It reads as a
    // test of nothing, and it is the one that fails if the ground check is inverted.
    [Fact]
    public void LeavesAStandingPlayerWhereTheyAre()
    {
        (PlayerTable players, EntityTable entities, PlayerFrame frame) = Standing();

        frame.Step(0, Input.Idle, Floor());

        Assert.Equal(StandX, players.X[0]);
        Assert.Equal(StandY, players.Y[0]);
        Assert.Equal(0xFF, entities.GroundState[0]);
    }

    // What $04BB and $05F5 leave a player at, which is where every case above starts.
    private static (PlayerTable Players, EntityTable Entities, PlayerFrame Frame) Standing()
    {
        PlayerTable players = new();
        EntityTable entities = new();

        players.State[0] = PlayerFrame.PlayingState;
        players.X[0] = StandX;
        players.Y[0] = StandY;

        entities.RiseCounter[0] = 0xFF;
        entities.FallCounter[0] = 0xFF;
        entities.GroundState[0] = 0xFF;
        entities.BubbleTimer[0] = 0xFF;

        PlayerSteer steer = new(players, entities);
        PlayerDescent descent = new(players, entities);

        PlayerFrame frame = new(
            players,
            entities,
            new PlayerMovement(players, entities),
            new PlayerJump(players, entities, new PlayerDrift(players, entities, steer), new PlayerLanding(players, entities, descent)),
            new PlayerFall(players, entities, steer));

        return (players, entities, frame);
    }

    private static SolidMap Open() => SolidMap.Build(Level());

    private static SolidMap Floor() => SolidMap.Build(Level((FloorRow, FloorColumn)));

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
