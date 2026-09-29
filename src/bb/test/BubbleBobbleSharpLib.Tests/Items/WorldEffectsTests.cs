// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
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

    // $2E61: every enemy in state 1 to $0A, or $0D, is killed, and the count is left at zero.
    [Fact]
    public void ClearingKillsEveryEnemyThatIsLeft()
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 3;
        new byte[] { 0x02, 0x0A, 0x0D, 0x0B, 0x00, 0x05 }.CopyTo(rig.Entities.State[2..]);

        rig.World.Apply(11, 0);

        Assert.Equal([0x0B, 0x0B, 0x0B, 0x0B, 0x00, 0x0B], rig.Entities.State[2..].ToArray());
        Assert.Equal(0, rig.Entities.EnemyCount);
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
        Assert.Equal(0x0B, rig.Entities.State[3]);
        Assert.Equal(0, rig.Entities.EnemyCount);
    }

    // $2DD7: the same, and the food and the special item are gone from the level; a second clear does nothing to the
    // objects, since `$58BF` is set.
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
            World = new(Clear, Flow, Timer, Entities, Objects, new(Entities, new Scores(), Random), Items);
        }

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
