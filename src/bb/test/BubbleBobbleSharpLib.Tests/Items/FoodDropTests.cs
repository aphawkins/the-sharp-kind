// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Enemies;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// $1A6F, $267B, $273D, $2921 and $29BB, worked by hand off the reference. The RNG is held at zero, so
// every roll is its smallest.
public sealed class FoodDropTests
{
    private const int Slot = 2;

    // The flat floor EnemyFrameTests stands its walker on, and the X and Y it stands at.
    private const byte StandX = 0x8C;
    private const byte StandY = 0xB5;

    private static readonly SolidMap s_floor = Map(18);
    private static readonly SolidMap s_thickFloor = Map(18, 19);

    // $1A7F to $1AB2.
    [Fact]
    public void KillRollsTheFlight()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        entities.State[Slot] = 0x03;
        entities.EnemyCount = 4;

        food.Kill(Slot - 2);

        Assert.Equal(FoodDrop.FlyingState, entities.State[Slot]);
        Assert.Equal(0x03, entities.LeftFlag[Slot]);
        Assert.Equal(3, entities.EnemyCount);
        Assert.Equal(0x01, entities.RiseCounter[Slot]);
        Assert.Equal(0x01, entities.FallCounter[Slot]);
        Assert.Equal(0x01, entities.RightFlag[Slot]);
        Assert.Equal(0x00, entities.GroundState[Slot]);
        Assert.Equal(0x07, entities.BubbleTimer[Slot]);
        Assert.Equal(0x09, entities.AttackTimer[Slot]);
        Assert.Equal(0x0E, entities.Colour[Slot]);
    }

    // $1A71. An enemy in state $0A has its class in RiseCounter.
    [Fact]
    public void KillTakesARisingEnemysClassFromItsRiseCounter()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        entities.State[Slot] = 0x0A;
        entities.RiseCounter[Slot] = 0x05;

        food.Kill(Slot - 2);

        Assert.Equal(0x05, entities.LeftFlag[Slot]);
    }

    // $1A7A. State $0D leaves the class that is there.
    [Fact]
    public void KillKeepsTheClassOfAnEnemyInStateD()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        entities.State[Slot] = 0x0D;
        entities.LeftFlag[Slot] = 0x06;

        food.Kill(Slot - 2);

        Assert.Equal(0x06, entities.LeftFlag[Slot]);
    }

    // $26BF. Sideways by RiseCounter and up by RightFlag.
    [Fact]
    public void FlightMovesAcrossAndUp()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0x80, y: 0x80);

        food.Fly(Slot);

        Assert.Equal(0x82, entities.X[Slot]);
        Assert.Equal(0x7D, entities.Y[Slot]);
        Assert.Equal(0x04, entities.BubbleTimer[Slot]);
    }

    // $26CC. The right wall stops it and turns it round.
    [Fact]
    public void FlightBouncesOffTheRightWall()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0xF3, y: 0x80);

        food.Fly(Slot);

        Assert.Equal(0xF4, entities.X[Slot]);
        Assert.Equal(0x01, entities.GroundState[Slot]);
    }

    // $26E1. And the left wall turns it back.
    [Fact]
    public void FlightBouncesOffTheLeftWall()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0x25, y: 0x80);
        entities.GroundState[Slot] = 0x01;

        food.Fly(Slot);

        Assert.Equal(0x24, entities.X[Slot]);
        Assert.Equal(0x00, entities.GroundState[Slot]);
    }

    // $26F9. Off the top, it comes back at the bottom.
    [Fact]
    public void FlightWrapsFromTheTopToTheBottom()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0x80, y: 0x16);

        food.Fly(Slot);

        Assert.Equal(0xF5, entities.Y[Slot]);
    }

    // $268B. The rise has run out, so the fall is rolled: 1 with bit 7 set. RightFlag is now 0, so
    // this frame does not move up or down.
    [Fact]
    public void FlightTurnsDownWhenTheRiseRunsOut()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0x80, y: 0x80);
        entities.BubbleTimer[Slot] = 0x00;
        entities.FallCounter[Slot] = 0x01;
        entities.RightFlag[Slot] = 0x01;

        food.Fly(Slot);

        Assert.Equal(0x03, entities.BubbleTimer[Slot]);
        Assert.Equal(0x81, entities.FallCounter[Slot]);
        Assert.Equal(0x00, entities.RightFlag[Slot]);
        Assert.Equal(0x80, entities.Y[Slot]);
        Assert.Equal(0x82, entities.X[Slot]);
    }

    // $26A9. A count equal to the fall's length goes on falling.
    [Fact]
    public void FlightFallsUntilTheCountPassesTheLength()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0x80, y: 0x80);
        entities.BubbleTimer[Slot] = 0x00;
        entities.FallCounter[Slot] = 0x82;
        entities.RightFlag[Slot] = 0x01;

        food.Fly(Slot);

        Assert.Equal(FoodDrop.FlyingState, entities.State[Slot]);
        Assert.Equal(0x82, entities.Y[Slot]);
    }

    // $26AF. Past it, the flight ends on an even X and an odd Y, without a move.
    [Fact]
    public void FlightEndsInTheFall()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0x81, y: 0x80);
        entities.BubbleTimer[Slot] = 0x00;
        entities.FallCounter[Slot] = 0x81;
        entities.RightFlag[Slot] = 0x01;

        food.Fly(Slot);

        Assert.Equal(FoodDrop.FallingState, entities.State[Slot]);
        Assert.Equal(0x80, entities.X[Slot]);
        Assert.Equal(0x81, entities.Y[Slot]);
    }

    // $2713. A quarter of the counter, offset by the class the enemy had: $AB89 holds 7 for class 2.
    [Theory]
    [InlineData(0x02, 0x00)]
    [InlineData(0x03, 0x08)]
    [InlineData(0x07, 0x09)]
    [InlineData(0x0B, 0x0A)]
    public void FlightFramesAreOffsetByTheClass(byte heading, byte frame)
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Flying(entities, x: 0x80, y: 0x80);
        entities.Heading[Slot] = heading;
        entities.FlashTimer[Slot] = 0xFF;

        food.Fly(Slot);

        Assert.Equal(frame, entities.Frame[Slot]);
        Assert.Equal(0x00, entities.FlashTimer[Slot]);
    }

    // $273D, through EnemyFrame. Two pixels a frame, and it lands where a walker stands.
    [Fact]
    public void FallLandsOnTheFloor()
    {
        (EntityTable entities, EnemyFrame frame) = Frame();
        Falling(entities, 0xA5);

        int frames = 0;

        while (entities.State[Slot] == FoodDrop.FallingState)
        {
            frame.Step(0, s_floor);
            frames++;
        }

        Assert.Equal(FoodDrop.LandedState, entities.State[Slot]);
        Assert.Equal(StandY, entities.Y[Slot]);
        Assert.Equal(8, frames);
    }

    // $2756. Inside a platform, it falls on through.
    [Fact]
    public void FallGoesOnThroughAPlatform()
    {
        (EntityTable entities, EnemyFrame frame) = Frame();
        Falling(entities, 0xBB);

        frame.Step(0, s_thickFloor);

        Assert.Equal(FoodDrop.FallingState, entities.State[Slot]);
        Assert.Equal(0xBD, entities.Y[Slot]);
    }

    // $2744. The bottom wraps to the top.
    [Fact]
    public void FallWrapsFromTheBottomToTheTop()
    {
        (EntityTable entities, EnemyFrame frame) = Frame();
        Falling(entities, 0xF3);

        frame.Step(0, s_floor);

        Assert.Equal(0x15, entities.Y[Slot]);
    }

    // $2921. AttackTimer 1 is food $08, and $A8E4's $0F for it is colour 7.
    [Fact]
    public void LandingTurnsTheEnemyIntoFood()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        entities.State[Slot] = FoodDrop.LandedState;
        entities.AttackTimer[Slot] = 0x01;

        food.Land(Slot);

        Assert.Equal(FoodDrop.FoodState, entities.State[Slot]);
        Assert.Equal(0xC8, entities.RiseCounter[Slot]);
        Assert.Equal(Slot, entities.Frame[Slot]);
        Assert.Equal(0xF2, entities.SpriteBase[Slot]);
        Assert.Equal(0x07, entities.Colour[Slot]);
    }

    // $2922. One a frame: slot 7 goes first, and slot 6 waits for the next.
    [Fact]
    public void OnlyOneEnemyBecomesFoodAFrame()
    {
        (EntityTable entities, EnemyFrame frame) = Frame();

        foreach (int slot in new[] { 6, 7 })
        {
            entities.State[slot] = FoodDrop.LandedState;
            entities.Mode[slot] = 0xFF;
            entities.AttackTimer[slot] = 0x01;
        }

        frame.Step(0, s_floor);

        Assert.Equal(FoodDrop.FoodState, entities.State[7]);
        Assert.Equal(FoodDrop.LandedState, entities.State[6]);

        frame.Step(0, s_floor);

        Assert.Equal(FoodDrop.FoodState, entities.State[6]);
    }

    // $29BE. Left alone, it goes when the count runs out.
    [Fact]
    public void FoodGoesWhenItsTimeRunsOut()
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Food(entities, 0x01);
        entities.RiseCounter[Slot] = 0x01;

        food.Wait(Slot);

        Assert.Equal(0, entities.State[Slot]);
        Assert.Equal(0, entities.X[Slot]);
        Assert.Equal(0, entities.Y[Slot]);
    }

    // $29FF. $50 goes in the lowest byte, and anything else in the byte above. Food 9's $0A is not
    // BCD, and the decimal add makes it $10.
    [Theory]
    [InlineData(0x01, 2, 0x50)]
    [InlineData(0x02, 1, 0x01)]
    [InlineData(0x09, 1, 0x10)]
    public void PlayerOneScoresTheFood(byte kind, int index, byte value)
    {
        (EntityTable entities, FoodDrop food, Scores scores) = Create();
        Food(entities, kind);
        Player(entities, 0, 0x01, StandX + 0x0F, StandY);

        food.Wait(Slot);

        Assert.Equal(value, scores.Bytes[index]);
        Assert.Equal(0, entities.State[Slot]);
    }

    // $29C0. Player two is looked at first, and takes it.
    [Fact]
    public void PlayerTwoIsFirst()
    {
        (EntityTable entities, FoodDrop food, Scores scores) = Create();
        Food(entities, 0x02);
        Player(entities, 0, 0x01, StandX, StandY);
        Player(entities, 1, 0x01, StandX, StandY);

        food.Wait(Slot);

        Assert.Equal(0x01, scores.Bytes[4]);
        Assert.Equal(0x00, scores.Bytes[1]);
    }

    // $29C7 and $29CB. A dying player still takes it, a dead one does not, and sixteen pixels is out
    // of reach.
    [Theory]
    [InlineData(0x0E, 0x00, true)]
    [InlineData(0x0F, 0x00, false)]
    [InlineData(0x01, 0x10, false)]
    public void OnlyAPlayerInReachTakesIt(byte state, byte offset, bool taken)
    {
        (EntityTable entities, FoodDrop food, _) = Create();
        Food(entities, 0x01);
        Player(entities, 0, state, StandX, (byte)(StandY - offset));

        food.Wait(Slot);

        Assert.Equal(taken, entities.State[Slot] == 0);
    }

    private static (EntityTable Entities, FoodDrop Food, Scores Scores) Create()
    {
        EntityTable entities = new();
        Scores scores = new();
        return (entities, new(entities, scores, new(new FakeRandomSource())), scores);
    }

    private static (EntityTable Entities, EnemyFrame Frame) Frame()
    {
        EntityTable entities = new();
        return (entities, TestEnemies.Frame(entities, new(), new(new FakeRandomSource())));
    }

    private static void Flying(EntityTable entities, byte x, byte y)
    {
        entities.State[Slot] = FoodDrop.FlyingState;
        entities.X[Slot] = x;
        entities.Y[Slot] = y;
        entities.RiseCounter[Slot] = 0x02;
        entities.FallCounter[Slot] = 0x03;
        entities.RightFlag[Slot] = 0x03;
        entities.BubbleTimer[Slot] = 0x05;
        entities.LeftFlag[Slot] = 0x02;
    }

    private static void Falling(EntityTable entities, byte y)
    {
        entities.State[Slot] = FoodDrop.FallingState;
        entities.Mode[Slot] = 0xFF;
        entities.X[Slot] = StandX;
        entities.Y[Slot] = y;
        entities.LeftFlag[Slot] = 0x02;
    }

    private static void Food(EntityTable entities, byte kind)
    {
        entities.State[Slot] = FoodDrop.FoodState;
        entities.X[Slot] = StandX;
        entities.Y[Slot] = StandY;
        entities.RiseCounter[Slot] = 0xC8;
        entities.AttackTimer[Slot] = kind;
    }

    private static void Player(EntityTable entities, int player, byte state, int x, byte y)
    {
        entities.State[player] = state;
        entities.X[player] = (byte)x;
        entities.Y[player] = y;
    }

    private static SolidMap Map(params int[] rows) => SolidMap.Build(
        new Level
        {
            Number = 1,
            Bitmap = [.. Enumerable.Range(0, 23).Select(row => rows.Contains(row) ? new string('#', 32) : new string('.', 32))],
        },
        []);
}
