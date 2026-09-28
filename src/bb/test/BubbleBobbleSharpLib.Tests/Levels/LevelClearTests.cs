// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// $0688, $16DA and $16E4, worked by hand off the reference. The clear itself is proved against VICE.
public sealed class LevelClearTests
{
    // $0688-$0690, $092A, $095E: level 1 has a bonus round after it, then every level after a step one longer.
    [Fact]
    public void BonusRoundsFollowTheLevelsTheScheduleNames()
    {
        Rig rig = new();
        List<int> bonus = [];

        for (byte subflg = 0; subflg < 30; subflg++)
        {
            rig.Clear.Begin(subflg);

            if (rig.Clear.BonusType != 0)
            {
                bonus.Add(subflg);
            }
        }

        Assert.Equal([0, 4, 9, 15, 22], bonus);
    }

    // $0688-$0690: the type is twice the level and nine, less forty-six while it is over.
    [Theory]
    [InlineData(0, 9)]
    [InlineData(4, 0x11)]
    [InlineData(9, 0x1B)]
    [InlineData(15, 0x27)]
    [InlineData(22, 0x35 - 0x2E)]
    public void TheBonusTypeIsTwiceTheLevelPlusNineWrapped(byte subflg, byte type)
    {
        Rig rig = new();
        rig.Clear.BonusLevel = subflg;

        rig.Clear.Begin(subflg);

        Assert.Equal(type, rig.Clear.BonusType);
    }

    // $1694-$16B5: the READY!! text running out leaves $A9B1 at $FF for good.
    [Fact]
    public void TheReadyTextComesOffOnce()
    {
        Rig rig = new();
        rig.Timer.Ready = 0;
        rig.Entities.EnemyCount = 5;

        rig.Clear.Update();

        Assert.Equal(LevelTimer.Stopped, rig.Timer.Ready);
    }

    // $16E0-$16E4: with one enemy left it is angry, whatever state $2D is in.
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(0xFF)]
    public void TheLastEnemyIsAngry(byte hurry)
    {
        Rig rig = new();
        rig.Timer.Hurry = hurry;
        rig.Entities.EnemyCount = 1;
        rig.Entities.State[2] = 0x02;
        rig.Entities.State[3] = 0x0B;

        rig.Clear.Update();

        Assert.Equal(0xFF, rig.Entities.FlashTimer[2]);
        Assert.Equal(0, rig.Entities.FlashTimer[3]);
    }

    // $16E2: more than one, and no one is made angry.
    [Fact]
    public void SeveralEnemiesAreLeftAlone()
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 2;
        rig.Entities.State[2] = 0x02;

        rig.Clear.Update();

        Assert.Equal(0, rig.Entities.FlashTimer[2]);
        Assert.Equal(0, rig.Timer.Hurry);
    }

    // $16DA-$16DC, $16F7: with $2D negative the level is already cleared, and a count of nothing does nothing more.
    [Fact]
    public void AClearedLevelIsNotClearedAgain()
    {
        Rig rig = new();
        rig.Timer.Hurry = 0xFF;
        rig.Timer.Seconds = 4;
        rig.Entities.EnemyCount = 0;

        rig.Clear.Update();

        Assert.Equal(4, rig.Timer.Seconds);
    }

    // $1751-$1770: a clear leaves nine seconds, the hurry-up state $FF and the clock running.
    [Fact]
    public void AClearLeavesNineSecondsOnTheClock()
    {
        Rig rig = new();
        rig.Timer.Seconds = 0x1C;
        rig.Timer.Frames = LevelTimer.Stopped;
        rig.Entities.EnemyCount = 0;

        rig.Clear.Update();

        Assert.Equal(9, rig.Timer.Seconds);
        Assert.Equal(0xFF, rig.Timer.Hurry);
        Assert.Equal(LevelTimer.FramesPerSecond, rig.Timer.Frames);
    }

    // $171B-$1750: an object that is not one of three stays what it was in $AA42 and becomes a pop.
    [Fact]
    public void AClearMakesEveryObjectButThreeAPop()
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 0;
        byte[] types = [0x04, 0x38, 0x41, 0x42, 0x44, 0x46, 0x4A, 0xFF, 0x3A, 0x00];
        types.CopyTo(rig.Objects.Type);

        rig.Clear.Update();

        Assert.Equal([0x3A, 0x3A, 0x3A, 0x3A, 0x44, 0x46, 0x4A, 0xFF, 0x3A, 0x3A], rig.Objects.Type[..types.Length].ToArray());
        Assert.Equal(0x04, rig.Objects.EnemyType[0]);
        Assert.Equal(0x3A, rig.Objects.EnemyType[8]);
        Assert.Equal(0, rig.Objects.EnemyType[4]);
    }

    // $1737-$1746: on a level with a bonus round, a bubble or special ($04, $38-$41) becomes $4C, with its
    // behaviour taken from the food colours.
    [Fact]
    public void ABonusLevelMakesItsBubblesAndSpecialsSomethingElse()
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 0;
        rig.Clear.BonusType = 9;
        new byte[] { 0x04, 0x38, 0x41, 0x42, 0x10 }.CopyTo(rig.Objects.Type);

        Assert.Throws<NotSupportedException>(rig.Clear.Update);

        Assert.Equal([0x4C, 0x4C, 0x4C, 0x3A, 0x3A], rig.Objects.Type[..5].ToArray());
        Assert.Equal(LevelItems.FoodColour(9), rig.Objects.Behaviour[0]);
        Assert.Equal(LevelItems.FoodColour(9), rig.Objects.Behaviour[2]);
    }

    // $16FB-$170B: a special item showing has its timer run out, and one not yet shown never comes.
    [Theory]
    [InlineData(0x18, true)]
    [InlineData(0x98, false)]
    public void AClearCallsTheSpecialItemOff(byte type, bool showing)
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 0;
        rig.Items.Type[1] = type;
        rig.Items.Timer[1] = 5;

        rig.Clear.Update();

        Assert.Equal(showing ? type : 0xFF, rig.Items.Type[1]);
        Assert.Equal(showing ? 0xFF : 5, rig.Items.Timer[1]);
    }

    // $170E-$171A: a food seed of $FF, which a Baron leaves, is the roll's low two bits; anything else is
    // the seconds left and eight.
    [Theory]
    [InlineData(0x00, 0x1C, 0x24)]
    [InlineData(0x7F, 0x02, 0x0A)]
    public void AClearSetsTheFoodSeedFromTheClock(byte seed, byte seconds, byte expected)
    {
        Rig rig = new();
        rig.Entities.EnemyCount = 0;
        rig.Items.FoodBase = seed;
        rig.Timer.Seconds = seconds;

        rig.Clear.Update();

        Assert.Equal(expected, rig.Items.FoodBase);
    }

    private sealed class Rig
    {
        internal Rig()
        {
            BbRandom random = new(new FakeRandomSource());
            Items = new(Entities, new Scores(), new(Players, Entities), random);
            Clear = new(Timer, Entities, Objects, Items, random);
            Timer.Begin(0);
        }

        internal LevelTimer Timer { get; } = new();

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal ObjectTable Objects { get; } = new();

        internal LevelItems Items { get; }

        internal LevelClear Clear { get; }
    }
}
