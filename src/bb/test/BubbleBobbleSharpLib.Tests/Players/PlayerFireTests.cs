// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// The three ways $22E8 is reached, and $2165 on the way back out. BubbleBlowTests proves what the
// blow does; these prove that the frame driver and the two stick handlers actually call it, which
// is the half of Phase 5 that lives in Phase 4's routines.
//
// The player stands at $44/$55 on a floor, the same start PlayerFrameTests uses.
public sealed class PlayerFireTests
{
    private const byte StandX = 0x44;
    private const byte StandY = 0x55;

    private const int FloorRow = 7;
    private const int FloorColumn = 6;

    private const byte Fire = Input.Idle & ~Input.Fire;
    private const byte Right = Input.Idle & ~Input.Right;
    private const byte Up = Input.Idle & ~Input.Up;
    private const byte RightAndFire = Right & Fire;
    private const byte UpAndFire = Up & Fire;

    private const int FirstSlot = ObjectTable.Capacity - 1;

    // $2223. Fire on the ground reaches the blow through $220C's last arm.
    [Fact]
    public void BlowsFromTheGround()
    {
        (_, EntityTable entities, _, PlayerFrame frame) = Standing();

        frame.Step(0, Fire, Floor());

        Assert.Equal(BubbleBlow.BlowFrames, entities.BubbleTimer[0]);
    }

    // $220C's arms are all taken in one pass, so a player walking and firing does both.
    [Fact]
    public void BlowsAndWalksOnTheSameFrame()
    {
        (PlayerTable players, EntityTable entities, _, PlayerFrame frame) = Standing();

        frame.Step(0, RightAndFire, Floor());

        Assert.Equal(StandX + 2, players.X[0]);
        Assert.Equal(BubbleBlow.BlowFrames, entities.BubbleTimer[0]);
    }

    // $220F. Up is a jmp to $222B rather than a jsr, so the fire arm is never reached on the frame
    // a jump starts - a player cannot jump and blow with one push.
    [Fact]
    public void DoesNotBlowOnTheFrameAJumpStarts()
    {
        (_, EntityTable entities, _, PlayerFrame frame) = Standing();

        frame.Step(0, UpAndFire, Floor());

        Assert.Equal(0x0F, entities.RiseCounter[0]);
        Assert.Equal(0xFF, entities.BubbleTimer[0]);
    }

    // $25F5. The steer's own fire arm, which is a jsr - a player blows and carries on steering. It
    // is called directly because reaching the steer through the arc takes a particular frame of a
    // particular jump, and that is PlayerSteerGoldenTests' job rather than this file's.
    [Fact]
    public void BlowsFromTheSteer()
    {
        (PlayerTable players, EntityTable entities, ObjectTable objects, _) = Standing();
        PlayerSteer steer = new(players, entities, new BubbleBlow(players, entities, objects));

        steer.Step(0, Fire, Floor());

        Assert.Equal(BubbleBlow.BlowFrames, entities.BubbleTimer[0]);
    }

    // $2165, and the whole chain end to end: one press, then nothing but the driver, and a bubble
    // in the table three frames later. This is the case that fails if $2301 is never called back.
    [Fact]
    public void MakesABubbleThreeFramesAfterThePress()
    {
        (_, _, ObjectTable objects, PlayerFrame frame) = Standing();
        SolidMap floor = Floor();

        frame.Step(0, Fire, floor);
        frame.Step(0, Input.Idle, floor);
        frame.Step(0, Input.Idle, floor);

        Assert.Equal(ObjectTable.FreeType, objects.Type[FirstSlot]);

        frame.Step(0, Input.Idle, floor);

        Assert.Equal(BubbleBlow.BubbleType, objects.Type[FirstSlot]);
    }

    // And the blow ends: seven frames after the press the timer is back at its idle, so the next
    // frame is an ordinary one again.
    [Fact]
    public void EndsTheBlowAfterSevenFrames()
    {
        (_, EntityTable entities, _, PlayerFrame frame) = Standing();
        SolidMap floor = Floor();

        frame.Step(0, Fire, floor);

        for (int tick = 0; tick < 7; tick++)
        {
            frame.Step(0, Input.Idle, floor);
        }

        Assert.Equal(0xFF, entities.BubbleTimer[0]);
        Assert.Equal(0x00, entities.Frame[0]);
    }

    private static (PlayerTable Players, EntityTable Entities, ObjectTable Objects, PlayerFrame Frame) Standing()
    {
        PlayerTable players = new();
        EntityTable entities = new();
        ObjectTable objects = new();

        players.State[0] = PlayerFrame.PlayingState;
        players.X[0] = StandX;
        players.Y[0] = StandY;

        entities.RiseCounter[0] = 0xFF;
        entities.FallCounter[0] = 0xFF;
        entities.GroundState[0] = 0xFF;
        entities.BubbleTimer[0] = 0xFF;

        BubbleBlow blow = new(players, entities, objects);
        PlayerSteer steer = new(players, entities, blow);
        PlayerDescent descent = new(players, entities);

        PlayerFrame frame = new(
            players,
            entities,
            new PlayerMovement(players, entities, blow),
            new PlayerJump(players, entities, new PlayerDrift(players, entities, steer), new PlayerLanding(players, entities, descent)),
            new PlayerFall(players, entities, steer),
            blow);

        return (players, entities, objects, frame);
    }

    private static SolidMap Floor() => SolidMap.Build(Level());

    private static Level Level()
    {
        List<string> bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)];
        bitmap[FloorRow - 1] = $"{bitmap[FloorRow - 1][..FloorColumn]}#{bitmap[FloorRow - 1][(FloorColumn + 1)..]}";

        return new() { Number = 1, Bitmap = bitmap };
    }
}
