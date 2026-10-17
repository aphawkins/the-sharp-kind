// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The last level's giant through the game loop. Its moves and its hits are proved against the game in BossFightOracleTests.
public sealed class BossFightTests
{
    private const byte Idle = 0xFF;

    private static readonly string s_assets = Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets");

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(s_assets, "Levels", "levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(s_assets, "Levels", "zones.json"));

    [Fact]
    public void TheGiantComesWhenTheReadyIsGone()
    {
        GameLoop loop = Start(100);

        Assert.False(loop.Boss.Active);

        Run(loop, () => loop.Boss.Active);

        Assert.True(loop.Boss.Active);
        Assert.Equal(LevelTimer.Stopped, loop.Timer.Frames);
    }

    [Fact]
    public void NoGiantComesOnAnotherLevel()
    {
        GameLoop loop = Start(1);

        for (int pass = 0; pass < 400; pass++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.False(loop.Boss.Active);
    }

    [Fact]
    public void TheLastLevelPatchesInThunderBubbles()
    {
        Assert.True(Start(100).Round.ThunderBlow);
        Assert.False(Start(1).Round.ThunderBlow);
    }

    [Fact]
    public void TheGiantKillsAPlayerItTouches()
    {
        GameLoop loop = Start(100);
        Run(loop, () => loop.Boss.Active);

        loop.Entities.Slot(0).State = PlayerFrame.PlayingState;
        loop.Entities.Slot(0).X = loop.Boss.X;
        loop.Entities.Slot(0).Y = loop.Boss.Y;
        loop.Boss.Move();

        Assert.Equal(PlayerFrame.DyingState, loop.Entities.Slot(0).State);
    }

    [Fact]
    public void AStunnedGiantIsWonByThePlayerWhoTouchesIt()
    {
        GameLoop loop = Start(100);
        Run(loop, () => loop.Boss.Active);

        loop.Boss.Frame = BossFight.StunnedFrame;
        loop.Entities.Slot(1).State = PlayerFrame.PlayingState;
        loop.Entities.Slot(1).X = loop.Boss.X;
        loop.Entities.Slot(1).Y = loop.Boss.Y;
        loop.Boss.Move();

        Assert.False(loop.Boss.Active);
        Assert.Equal(1, loop.Boss.Winner);
    }

    private static GameLoop Start(int number)
    {
        GameLoop loop = new(new(new RandomSource(new Random(1))), 2);
        loop.Start(s_levels.Level(number), s_zones.Zones(number), number);

        return loop;
    }

    private static void Run(GameLoop loop, Func<bool> until)
    {
        for (int pass = 0; pass < 1000 && !until(); pass++)
        {
            loop.Pass([Idle, Idle]);
        }
    }
}
