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
        entities.State[Slot] = 0;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(StandX, entities.X[Slot]);
        Assert.Equal(0, entities.AnimationTimer[Slot]);
    }

    // $1CA0. Two pixels a frame towards the row AttackTimer holds, animating as it goes.
    [Fact]
    public void DropsIntoTheLevelTwoPixelsAFrame()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Mode[Slot] = 0x00;
        entities.Y[Slot] = 0x15;
        entities.AttackTimer[Slot] = 0x19;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x17, entities.Y[Slot]);
        Assert.Equal(0x00, entities.Mode[Slot]);
        Assert.Equal(1, entities.AnimationTimer[Slot]);
    }

    // $1CAD. At the row, Mode goes negative, the bubble timer is parked and the first shot is ten
    // frames off. The thing does not move on the frame it arrives.
    [Fact]
    public void EndsTheDropAtItsRow()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Mode[Slot] = 0x00;
        entities.Y[Slot] = 0x19;
        entities.AttackTimer[Slot] = 0x19;
        entities.BubbleTimer[Slot] = 0x00;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x19, entities.Y[Slot]);
        Assert.Equal(0xFF, entities.Mode[Slot]);
        Assert.Equal(0xFF, entities.BubbleTimer[Slot]);
        Assert.Equal(0x0A, entities.AttackTimer[Slot]);
    }

    // $17BE. The spawned row becomes the drop's target, and twenty-two frames of two pixels take the
    // thing from $15 to $41, short of a row further down. $1CA0 finishes the drop in play.
    [Fact]
    public void EntersFromTheTopTowardsItsRow()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();

        frame.Enter();

        Assert.Equal(StandY, entities.AttackTimer[Slot]);
        Assert.Equal(0x41, entities.Y[Slot]);
        Assert.Equal(0x00, entities.Mode[Slot]);
    }

    // $17E7. The loop does not look at the mode, so a thing that reaches its row takes it down once
    // on each frame left. $25 is eight frames from $15, which leaves fourteen.
    [Theory]
    [InlineData(0x25, 0xF2)]
    [InlineData(0x15, 0xEA)]
    public void TakesTheModeDownOnEveryFrameAtItsRow(byte row, byte mode)
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Y[Slot] = row;

        frame.Enter();

        Assert.Equal(row, entities.Y[Slot]);
        Assert.Equal(row, entities.AttackTimer[Slot]);
        Assert.Equal(mode, entities.Mode[Slot]);
    }

    // $17C2. Every enemy slot is put at the top, but only a live one drops.
    [Fact]
    public void PutsAnEmptySlotAtTheTopWithoutMovingIt()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.Y[Slot + 1] = 0x75;

        frame.Enter();

        Assert.Equal(0x75, entities.AttackTimer[Slot + 1]);
        Assert.Equal(0x15, entities.Y[Slot + 1]);
        Assert.Equal(0x00, entities.Mode[Slot + 1]);
    }

    // $1D11. The spawn delay counts down, and the thing animates on the spot without walking.
    [Fact]
    public void HoldsTheThingWhileTheDelayRuns()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.HoldTimer[Slot] = 0x02;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x01, entities.HoldTimer[Slot]);
        Assert.Equal(StandX, entities.X[Slot]);
        Assert.Equal(1, entities.AnimationTimer[Slot]);
    }

    [Fact]
    public void WalksOnceTheDelayIsOver()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();

        frame.Step(Slot, 0, s_map);

        Assert.Equal(StandX - 2, entities.X[Slot]);
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
        entities.FlashTimer[Slot] = 0xFF;

        frame.Step(Slot, (byte)counter, s_map);

        Assert.Equal(x, entities.X[Slot]);
    }

    // $1D03 comes before $1D11, so an angry thing still held by its delay takes the extra step.
    [Fact]
    public void AngerStepsEvenDuringTheDelay()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.FlashTimer[Slot] = 0xFF;
        entities.HoldTimer[Slot] = 0x02;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(StandX - 2, entities.X[Slot]);
        Assert.Equal(0x01, entities.HoldTimer[Slot]);
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
        entities.State[0] = (byte)playerZeroState;
        entities.Y[0] = 0xDD;
        entities.State[1] = 0x01;
        entities.Y[1] = StandY;

        frame.Step(slot, 0, s_map);

        Assert.Equal(leap, entities.LeapFlag[slot]);
    }

    // $1CBD's loop runs from slot 7 down to slot 2 and no further.
    [Fact]
    public void RunsEveryEnemySlotAndNoPlayer()
    {
        (EntityTable entities, EnemyFrame frame) = Walker(7);
        Place(entities, 0);

        frame.Step(0, s_map);

        Assert.Equal(StandX - 2, entities.X[7]);
        Assert.Equal(StandX, entities.X[0]);
    }

    // $1E87 for a slot not free to move: state 9 flips bit 0, the rest step the walk cycle.
    [Fact]
    public void TogglesStateNineWhileHeld()
    {
        (EntityTable entities, EnemyFrame frame) = Walker();
        entities.State[Slot] = 0x09;
        entities.HoldTimer[Slot] = 0x02;
        entities.AnimationTimer[Slot] = 0x01;
        entities.Frame[Slot] = 0x06;

        frame.Step(Slot, 0, s_map);

        Assert.Equal(0x07, entities.Frame[Slot]);
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
        entities.State[0] = 0x01;
        entities.Y[0] = 0xDD;
        entities.State[slot] = 0x02;
        entities.X[slot] = StandX;
        entities.Y[slot] = StandY;
        entities.Mode[slot] = 0xFF;
        entities.Heading[slot] = 0x01;
        entities.Frame[slot] = 0x04;
        entities.FrameCount[slot] = 0x04;
        entities.FrameMask[slot] = 0x03;
        entities.RiseCounter[slot] = 0xFF;
        entities.FallCounter[slot] = 0xFF;
        entities.GroundState[slot] = 0xFF;
    }
}
