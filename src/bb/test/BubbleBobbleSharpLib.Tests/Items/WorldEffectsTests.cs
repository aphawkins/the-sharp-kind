// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Specials;
using BubbleBobbleSharpLib.Tests.Levels;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// The special-item effects that act on the level, worked by hand from `$2DD7`, `$2ECC`, `$2E61`, `$2E79`, `$7FA4` and
// `$7FA7`. The ones that set a few bytes are also proved against the game, in WorldEffectsOracleTests.
public sealed class WorldEffectsTests
{
    // $7FA4 and $7FA7: three or seven levels on, no further than the last.
    [Theory]
    [InlineData(20, 5, 8)]
    [InlineData(22, 5, 12)]
    [InlineData(20, 98, 99)]
    [InlineData(22, 97, 99)]
    public void SkipsLevelsToNoFurtherThanTheLast(int type, byte level, int target)
    {
        Rig rig = new() { World = { Level = level } };

        rig.World.Apply(type, 0);

        Assert.Equal(target, rig.World.SkipTo);
    }

    // $7C37: the last milestone's item makes the game go to level $45 (70), from wherever it is.
    [Theory]
    [InlineData(48)]
    [InlineData(5)]
    public void TheFinalMilestoneItemSendsTheGameToLevelSeventy(byte level)
    {
        Rig rig = new() { World = { Level = level } };

        rig.World.Apply(33, 0);

        Assert.Equal(0x45, rig.World.SkipTo);
    }

    // $2E61: every enemy in state 1 to $0A, or $0D, is killed, and the count is left at zero; `$2ECC` then keeps the
    // states for the show's end, makes the slots state $13 and counts one enemy, the show.
    [Fact]
    public void ClearingKillsEveryEnemyThatIsLeft()
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 3;
        rig.Entities.State[2..].Load([0x02, 0x0A, 0x0D, 0x0B, 0x00, 0x05]);

        rig.World.Apply(11, 0);

        Assert.Equal([0x0B, 0x0B, 0x0B, 0x0B, 0x00, 0x0B], rig.Show.KeptState.ToArray());
        Assert.All(rig.Entities.State[2..].ToArray(), state => Assert.Equal(SpriteShow.DiamondState, state));
        Assert.Equal(1, rig.Entities.EnemyCount);
        Assert.NotNull(rig.World.Hold);
    }

    // $2E79: an enemy in a bubble ($18-$23) is let out, and killed with the rest; every object becomes a $4A.
    [Fact]
    public void ClearingLetsCaughtEnemiesOutAndResetsEveryObject()
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 2;
        rig.Entities.State[3] = 0x0B;
        rig.Objects.Type[4] = 0x1A;
        rig.Objects.Variant[4] = 0x03;
        rig.Objects.Column[4] = 9;
        rig.Objects.Row[4] = 7;
        rig.Objects.Type[5] = 0x04;

        rig.World.Apply(11, 0);

        Assert.All(rig.Objects.Type.ToArray(), type => Assert.Equal(0x4A, type));
        Assert.Equal(0, rig.Objects.Column[4]);
        Assert.Equal(0, rig.Objects.Row[4]);
        Assert.Equal(0x0B, rig.Show.KeptState[1]);
    }

    // $2DD7: the objects are reset, and the food and the special item are gone from the level; a second clear does nothing
    // to the objects, since `$58BF` is set.
    [Fact]
    public void ClearingWithTheWipeAlsoHidesTheItemsAndOnlyResetsOnce()
    {
        Rig rig = new();
        rig.Items.Type[0] = 0x10;
        rig.Items.Type[1] = 0x1E;
        rig.Objects.Type[0] = 0x04;

        rig.World.Apply(8, 0);
        rig.Objects.Type[0] = 0x04;
        rig.World.Apply(11, 0);

        Assert.Equal(0xFF, rig.Items.Type[0]);
        Assert.Equal(0xFF, rig.Items.Type[1]);
        Assert.Equal(0x04, rig.Objects.Type[0]);
        Assert.True(rig.World.Cleared);
    }

    // $7BA6: the wipe puts the fire out and stops the water before anything else.
    [Fact]
    public void TheWipeStopsTheFireAndTheWater()
    {
        Rig rig = new();
        rig.Water.Mask = 0xFF;
        rig.Fire.Light(2, 4, 5, 6);

        rig.World.Apply(8, 0);

        Assert.Equal(0, rig.Water.Mask);
        Assert.All(rig.Fire.Timers.ToArray(), timer => Assert.Equal(FireStrips.Free, timer));
    }

    // $2E15-$2E78: twenty-five rows, two frames each, from the bottom, and only then are the enemies killed and a player in the
    // water put on its feet.
    [Fact]
    public void TheWipeTakesTwoFramesARowAndKillsTheEnemiesAfterIt()
    {
        Rig rig = new();
        rig.Entities.State[0] = WaterFlow.CarriedState;
        rig.Entities.State[1] = 0x01;
        rig.Entities.State[4] = 0x02;
        rig.Entities.EnemyCount = 1;

        rig.World.Apply(8, 0);
        IGameHold hold = rig.World.Hold!;
        int frames = 0;

        while (hold.Frame())
        {
            frames++;

            if (frames % 2 == 0)
            {
                hold.Pass();
                Assert.Equal(frames / 2, rig.World.Wipe.Wiped);
                Assert.Equal(0x02, rig.Entities.State[4]);
            }
        }

        hold.Finish(0);

        Assert.Equal(50, frames);
        Assert.Equal(ScreenWipe.Rows, rig.World.Wipe.Wiped);
        Assert.False(hold.Paused);
        Assert.Equal(0x01, rig.Entities.State[0]);
        Assert.Equal(FoodDrop.FlyingState, rig.Entities.State[4]);
        Assert.Equal(0, rig.Entities.EnemyCount);
    }

    // $2F68-$2F7C.
    [Fact]
    public void TheFreezeSwapsTheColoursAndStopsTheClock()
    {
        Rig rig = new();
        rig.Flow.Begin(0x69);
        rig.Timer.Frames = 0x20;

        rig.World.Apply(15, 0);

        Assert.Equal(0x87, rig.Flow.Freeze);
        Assert.Equal(0x1E, rig.Flow.Colours);
        Assert.Equal(LevelTimer.Stopped, rig.Timer.Frames);
    }

    // $348A-$3510.
    [Fact]
    public void AnEffectMakesABonusRoundDue()
    {
        Rig rig = new();

        rig.World.Apply(26, 1);

        Assert.Equal(0x2B, rig.Clear.BonusType);
        Assert.Equal(0x40, rig.Clear.BonusArg);
        Assert.Equal(3, rig.Clear.BonusStage);
    }

    private sealed class Rig
    {
        internal Rig()
        {
            Random = new(new ScriptedRandomSource([]));
            Flow = new(Timer);
            Items = new(Entities, new Scores(), new ItemEffects(new PlayerTable(), Entities), Random);
            Clear = new(Timer, Entities, Objects, Items, Random);
            FoodDrop food = new(Entities, new Scores(), Random);
            Fire = new(Entities, food);
            Water = new(Entities, Random);
            Show = new(Entities, Timer);
            World = new(Clear, Flow, Timer, Entities, Objects, food, Items, Fire, Water, Show);
        }

        internal FireStrips Fire { get; }

        internal WaterFlow Water { get; }

        internal SpriteShow Show { get; }

        internal EntityTable Entities { get; } = new();

        internal ObjectTable Objects { get; } = new();

        internal LevelTimer Timer { get; } = new();

        internal BbRandom Random { get; }

        internal LevelFlow Flow { get; }

        internal LevelItems Items { get; }

        internal LevelClear Clear { get; }

        internal WorldEffects World { get; }
    }
}
