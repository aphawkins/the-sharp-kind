// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// $1CDB to $1D21 for the enemy slots, worked by hand off the reference. The golden captures go
// through the plain $1E6C arm only, so the drop, the hold and the anger are proved here.
public sealed class EnemyFrameTests
{
    private const int Slot = 2;

    // One flat floor, on bitmap row 18. Column 15, and a Y on a row boundary above it.
    private const byte StandX = 0x8C;
    private const byte StandY = 0xB5;

    private static readonly SolidMap s_map = SolidMap.Build(
        new Level
        {
            Number = 1,
            Bitmap = [.. Enumerable.Range(0, 23).Select(row => row == 18 ? new string('#', 32) : new string('.', 32))],
        },
        []);

    [Fact]
    public void LeavesAnEmptySlotAlone()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).State = 0;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(StandX, entities.Slot(Slot).X);
        Assert.Equal(0, entities.Slot(Slot).AnimationTimer);
    }

    // $1CA0. Two pixels a frame towards the row AttackTimer holds, animating as it goes.
    [Fact]
    public void DropsIntoTheLevelTwoPixelsAFrame()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).Mode = 0x00;
        entities.Slot(Slot).Y = 0x15;
        entities.Slot(Slot).AttackTimer = 0x19;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x17, entities.Slot(Slot).Y);
        Assert.Equal(0x00, entities.Slot(Slot).Mode);
        Assert.Equal(1, entities.Slot(Slot).AnimationTimer);
    }

    // $1CAD. At the row, Mode goes negative, the bubble timer is parked and the first shot is ten
    // frames off. The thing does not move on the frame it arrives.
    [Fact]
    public void EndsTheDropAtItsRow()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).Mode = 0x00;
        entities.Slot(Slot).Y = 0x19;
        entities.Slot(Slot).AttackTimer = 0x19;
        entities.Slot(Slot).BubbleTimer = 0x00;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x19, entities.Slot(Slot).Y);
        Assert.Equal(0xFF, entities.Slot(Slot).Mode);
        Assert.Equal(0xFF, entities.Slot(Slot).BubbleTimer);
        Assert.Equal(0x0A, entities.Slot(Slot).AttackTimer);
    }

    // $17BE. The spawned row becomes the drop's target, and twenty-two frames of two pixels take the
    // thing from $15 to $41, short of a row further down. $1CA0 finishes the drop in play.
    [Fact]
    public void EntersFromTheTopTowardsItsRow()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();

        frame.Enter();

        Assert.Equal(StandY, entities.Slot(Slot).AttackTimer);
        Assert.Equal(0x41, entities.Slot(Slot).Y);
        Assert.Equal(0x00, entities.Slot(Slot).Mode);
    }

    // $17E7. The loop does not look at the mode, so a thing that reaches its row takes it down once
    // on each frame left. $25 is eight frames from $15, which leaves fourteen.
    [Theory]
    [InlineData(0x25, 0xF2)]
    [InlineData(0x15, 0xEA)]
    public void TakesTheModeDownOnEveryFrameAtItsRow(byte row, byte mode)
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).Y = row;

        frame.Enter();

        Assert.Equal(row, entities.Slot(Slot).Y);
        Assert.Equal(row, entities.Slot(Slot).AttackTimer);
        Assert.Equal(mode, entities.Slot(Slot).Mode);
    }

    // $17C2. Every enemy slot is put at the top, but only a live one drops.
    [Fact]
    public void PutsAnEmptySlotAtTheTopWithoutMovingIt()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot + 1).Y = 0x75;

        frame.Enter();

        Assert.Equal(0x75, entities.Slot(Slot + 1).AttackTimer);
        Assert.Equal(0x15, entities.Slot(Slot + 1).Y);
        Assert.Equal(0x00, entities.Slot(Slot + 1).Mode);
    }

    // $1D11. The spawn delay counts down, and the thing animates on the spot without walking.
    [Fact]
    public void HoldsTheThingWhileTheDelayRuns()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).HoldTimer = 0x02;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x01, entities.Slot(Slot).HoldTimer);
        Assert.Equal(StandX, entities.Slot(Slot).X);
        Assert.Equal(1, entities.Slot(Slot).AnimationTimer);
    }

    [Fact]
    public void WalksOnceTheDelayIsOver()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();

        frame.Step(Slot, 0, s_map);

        Assert.Equal(StandX - 2, entities.Slot(Slot).X);
    }

    // $1D03. An angry thing takes a second step on frames whose counter has bit 1 clear.
    [Theory]
    [InlineData(0x00, StandX - 4)]
    [InlineData(0x01, StandX - 4)]
    [InlineData(0x02, StandX - 2)]
    [InlineData(0x03, StandX - 2)]
    public void StepsTwiceWhenAngryOnHalfTheFrames(int counter, int x)
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).FlashTimer = 0xFF;

        frame.Step(Slot, (byte)counter, s_map);

        Assert.Equal(x, entities.Slot(Slot).X);
    }

    // $1D03 comes before $1D11, so an angry thing still held by its delay takes the extra step.
    [Fact]
    public void AngerStepsEvenDuringTheDelay()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).FlashTimer = 0xFF;
        entities.Slot(Slot).HoldTimer = 0x02;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(StandX - 2, entities.Slot(Slot).X);
        Assert.Equal(0x01, entities.Slot(Slot).HoldTimer);
    }

    // $1CE3. The player of the slot's own parity if they are playing, else the other one. Player 1 is
    // level with the thing, so following them lets it leap. Player 0 is below it.
    [Theory]
    [InlineData(3, 0x01, 0xFF)]
    [InlineData(2, 0x01, 0x00)]
    [InlineData(2, 0x0E, 0xFF)]
    public void FollowsThePlayerOnItsOwnSideIfTheyArePlaying(int slot, int playerZeroState, int leap)
    {
        (EntityTable entities, EnemyFrame frame) = Walker(slot);
        entities.Slot(0).State = (byte)playerZeroState;
        entities.Slot(0).Y = 0xDD;
        entities.Slot(1).State = 0x01;
        entities.Slot(1).Y = StandY;

        frame.Step(slot, 0, s_map);

        Assert.Equal(leap, entities.Slot(slot).LeapFlag);
    }

    // $1CBD's loop runs from slot 7 down to slot 2 and no further.
    [Fact]
    public void RunsEveryEnemySlotAndNoPlayer()
    {
        (EntityTable entities, EnemyFrame frame) = Walker(7);
        Place(entities, 0);

        frame.Step(0, s_map);

        Assert.Equal(StandX - 2, entities.Slot(7).X);
        Assert.Equal(StandX, entities.Slot(0).X);
    }

    // $1E87 for a slot not free to move: state 9 flips bit 0, the rest step the walk cycle.
    [Fact]
    public void TogglesStateNineWhileHeld()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Slot(Slot).State = 0x09;
        entities.Slot(Slot).HoldTimer = 0x02;
        entities.Slot(Slot).AnimationTimer = 0x01;
        entities.Slot(Slot).Frame = 0x06;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x07, entities.Slot(Slot).Frame);
    }

    // $1CF5-$1D01: while $67 is set, an enemy not yet out of its bubble (state 1 to $0A) takes no turn.
    [Theory]
    [InlineData(0x02)]
    [InlineData(0x0A)]
    public void AFrozenEnemyDoesNotMove(byte state)
    {
        LevelFlow flow = new(new()) { Freeze = 0x40 };
        EntityTable entities = new();
        Place(entities, Slot);
        entities.Slot(Slot).State = state;
        EnemyFrame frame = TestEnemies.Frame(entities, new(), new(new FakeRandomSource()), new(), flow);

        frame.Step(Slot, 0, s_map);

        Assert.Equal(StandX, entities.Slot(Slot).X);
        Assert.Equal(0, entities.Slot(Slot).AnimationTimer);
    }

    // The same enemy moves once the freeze has run out.
    [Fact]
    public void ANotFrozenEnemyWalks()
    {
        LevelFlow flow = new(new()) { Freeze = 0 };
        EntityTable entities = new();
        Place(entities, Slot);
        EnemyFrame frame = TestEnemies.Frame(entities, new(), new(new FakeRandomSource()), new(), flow);

        frame.Step(Slot, 0, s_map);

        Assert.NotEqual(StandX, entities.Slot(Slot).X);
    }

    // $1CF5-$1D01: an enemy from state $0B up (in its bubble, popped, food) is not held by the freeze.
    [Theory]
    [InlineData(0x0B)]
    [InlineData(0x0C)]
    public void TheFreezeLeavesAnEnemyPastStateTenAlone(byte state)
    {
        byte[] frozen = Run(state, 0x40);
        byte[] free = Run(state, 0);

        Assert.Equal(Convert.ToHexString(free), Convert.ToHexString(frozen));
        Assert.NotEqual(Convert.ToHexString(Run(state, 0)), Convert.ToHexString(new byte[free.Length]));
    }

    // $1CDB-$1CF3: an enemy still dropping in is not held by the freeze either.
    [Fact]
    public void TheFreezeDoesNotStopAnEnemyDroppingIn()
    {
        LevelFlow flow = new(new()) { Freeze = 0x40 };
        EntityTable entities = new();
        Place(entities, Slot);
        entities.Slot(Slot).Mode = 0x00;
        entities.Slot(Slot).Y = 0x15;
        entities.Slot(Slot).AttackTimer = 0x19;
        EnemyFrame frame = TestEnemies.Frame(entities, new(), new(new FakeRandomSource()), new(), flow);

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x17, entities.Slot(Slot).Y);
    }

    private static byte[] Run(byte state, byte freeze)
    {
        LevelFlow flow = new(new()) { Freeze = freeze };
        EntityTable entities = new();
        Place(entities, Slot);
        entities.Slot(Slot).State = state;
        entities.Slot(Slot).Y = 0x80;
        EnemyFrame frame = TestEnemies.Frame(entities, new(), new(new FakeRandomSource()), new(), flow);

        frame.Step(Slot, 0, s_map);

        return [entities.Slot(Slot).X, entities.Slot(Slot).Y, entities.Slot(Slot).Frame, entities.Slot(Slot).State];
    }

    private static (EntityTable Entities, EnemyFrame Frame) Walker(int slot = Slot)
    {
        EntityTable entities = new();
        Place(entities, slot);
        return (entities, TestEnemies.Frame(entities, new(), new(new FakeRandomSource())));
    }

    // A class 0 thing walking left on the floor, free to move, with a player below it.
    private static void Place(EntityTable entities, int slot)
    {
        entities.Slot(0).State = 0x01;
        entities.Slot(0).Y = 0xDD;
        entities.Slot(slot).State = 0x02;
        entities.Slot(slot).X = StandX;
        entities.Slot(slot).Y = StandY;
        entities.Slot(slot).Mode = 0xFF;
        entities.Slot(slot).Heading = 0x01;
        entities.Slot(slot).Frame = 0x04;
        entities.Slot(slot).FrameCount = 0x04;
        entities.Slot(slot).FrameMask = 0x03;
        entities.Slot(slot).RiseCounter = 0xFF;
        entities.Slot(slot).FallCounter = 0xFF;
        entities.Slot(slot).GroundState = 0xFF;
    }
}
