// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
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

        // $1D32 can kill the player, and nothing takes a dead player on from $0F yet.
        Assert.Contains(loop.Entities.State[0], (byte[])[0x01, 0x0E, 0x0F]);
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

    // $05F5 readies all eight slots, not just the players: no jump, no fall, on the ground, out of a
    // bubble, with no leap, no climb and no flash for an enemy.
    [Fact]
    public void StartsEverySlotIdle()
    {
        GameLoop loop = Start(1, seed: 1);
        EntityTable entities = loop.Entities;

        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            Assert.Equal(0xFF, entities.RiseCounter[slot]);
            Assert.Equal(0xFF, entities.FallCounter[slot]);
            Assert.Equal(0xFF, entities.GroundState[slot]);
            Assert.Equal(0xFF, entities.BubbleTimer[slot]);
            Assert.Equal(0x00, entities.LeapFlag[slot]);
            Assert.Equal(0x00, entities.ClimbFlag[slot]);
            Assert.False(entities.ClimbNext[slot]);
        }

        for (int slot = PlayerTable.Capacity; slot < EntityTable.Capacity; slot++)
        {
            Assert.Equal(0x00, entities.FlashTimer[slot]);
        }
    }

    // Level 1's enemies start at the top and fall onto the top platform, where VICE has them at $65.
    // They need $17BE's drop target and $05F5's idle jump counters to stop there, not fall through.
    [Fact]
    public void Level1EnemiesLandOnTheTopPlatform()
    {
        const byte TopPlatform = 0x65;
        const int LandingPasses = 95;

        GameLoop loop = Start(1, seed: 1);
        int last = 1 + s_levels.Level(1).Enemies.Count;

        for (int pass = 0; pass < LandingPasses; pass++)
        {
            loop.Pass([Idle, Idle]);

            for (int slot = 2; slot <= last; slot++)
            {
                Assert.InRange(loop.Entities.Y[slot], 0x15, TopPlatform);
            }
        }

        for (int slot = 2; slot <= last; slot++)
        {
            Assert.Equal(TopPlatform, loop.Entities.Y[slot]);
            Assert.Equal(0xFF, loop.Entities.GroundState[slot]);
        }
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
