// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests;

// The game loop over every level: what the port has translated, run together in the reference's
// order, for two minutes of play from each level's start.
[Trait("Level", "Integration")]
public sealed class GameLoopTests
{
    // Two minutes at twenty-five passes a second.
    private const int Passes = 3000;

    // A port byte reads $FF idle, and a bit goes clear while its direction is pushed.
    private const byte Idle = 0xFF;
    private const byte PushRight = Idle & unchecked((byte)~0x08);
    private const byte PushLeft = Idle & unchecked((byte)~0x04);
    private const byte PushUp = Idle & unchecked((byte)~0x01);
    private const byte PushFire = Idle & unchecked((byte)~0x10);

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly ZoneStore s_zones = ZoneStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "zones.json"));

    public static TheoryData<int> Levels()
    {
        TheoryData<int> levels = [];

        for (int level = 1; level <= LevelStore.Count; level++)
        {
            levels.Add(level);
        }

        return levels;
    }

    // Idle, then the stick in a fixed pattern: walk, jump, blow, in both directions.
    //
    // A run may stop early on one thing only: a routine the port has not translated, which throws
    // NotSupportedException and names its address - a special item's effect, most often. Anything
    // else thrown is a fault.
    [Theory]
    [MemberData(nameof(Levels))]
    public void RunsTwoMinutesFromTheStartOfEveryLevel(int number)
    {
        GameLoop loop = Start(number, seed: number);

        try
        {
            for (int pass = 0; pass < Passes; pass++)
            {
                loop.Pass([Stick(pass), Idle]);
            }
        }
        catch (NotSupportedException gap)
        {
            Assert.Contains("is not translated", gap.Message, StringComparison.Ordinal);
            return;
        }

        // Nothing translated can kill a player yet, so they are still playing.
        Assert.Equal(1, loop.Entities.State[0]);
    }

    // A level starts with its enemies in slots 2 onwards, both items hidden, and the one player up.
    [Fact]
    public void StartsALevel()
    {
        GameLoop loop = Start(1, seed: 1);

        Assert.Equal(1, loop.Entities.State[0]);
        Assert.Equal(0, loop.Entities.State[1]);
        Assert.Equal(s_levels.Level(1).Enemies.Count, loop.Entities.EnemyCount);
        Assert.All(loop.Items.Type.ToArray(), t => Assert.True(t >= 0x80));
        Assert.All(loop.Objects.Type.ToArray(), t => Assert.Equal(ObjectTable.FreeType, t));
    }

    // $1CBD runs in every pass, so a held stick moves the player a walk step a pass.
    [Fact]
    public void MovesThePlayerEachPass()
    {
        GameLoop loop = Start(1, seed: 1);
        byte x = loop.Entities.X[0];

        loop.Pass([PushRight, Idle]);
        loop.Pass([PushRight, Idle]);

        Assert.NotEqual(x, loop.Entities.X[0]);
    }

    // Fire makes a bubble three passes later, and the loop then carries it away from the player.
    [Fact]
    public void BlowsAndMovesABubble()
    {
        GameLoop loop = Start(1, seed: 1);
        const int slot = ObjectTable.Capacity - 1;

        loop.Pass([PushFire, Idle]);

        int pass = 1;

        while (loop.Objects.Type[slot] == ObjectTable.FreeType && pass++ < 10)
        {
            loop.Pass([Idle, Idle]);
        }

        byte x = loop.Objects.X[slot];

        for (int i = 0; i < 10; i++)
        {
            loop.Pass([Idle, Idle]);
        }

        Assert.NotEqual(ObjectTable.FreeType, loop.Objects.Type[slot]);
        Assert.NotEqual(x, loop.Objects.X[slot]);
    }

    // Two seconds of each.
    private static byte Stick(int pass)
    {
        int phase = pass / 50;

        return (phase % 6) switch
        {
            0 => Idle,
            1 => PushRight,
            2 => PushRight & PushUp,
            3 => PushFire,
            4 => PushLeft,
            _ => PushLeft & PushFire,
        };
    }

    private static GameLoop Start(int number, int seed)
    {
        GameLoop loop = new(new(new RandomSource(new Random(seed))), 1);
        loop.Start(s_levels.Level(number), s_zones.Zones(number), number);
        return loop;
    }
}
