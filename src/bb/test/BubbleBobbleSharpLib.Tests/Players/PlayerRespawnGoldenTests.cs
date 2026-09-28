// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// A player respawning against the real thing.
//
// Captures/respawn-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1 with one
// player standing idle until the enemies had taken every life. No intervention. Four exec checkpoints:
// $04A0, where $045C finds a dead player, and $0A1F, where it returns (R lines); and $1CDB and $1D24,
// both with X < 2, bracketing a player slot's turn in $1CBD, kept only when the slot was in state $10
// (S lines). Three respawns of 74 turns each, and a fourth death, the game over.
//
// Each line is the bytes before and after: $08, $22, $B2 to $C9, then $8520, $8548, $8610, $8688,
// $86D8, $8728, $87A0, $87C8 and $87F0 for all eight slots, then $045A-$045B, $A77B-$A782, $61-$66,
// $4A and $5B7F. The level timer's $2A-$2D, which $0504 writes, are not captured; nor is $8818.
[Trait("Level", "Integration")]
public sealed class PlayerRespawnGoldenTests
{
    private const int Counter = 0;
    private const int Slot = 1;
    private const int States = 2;
    private const int Xs = States + EntityTable.Capacity;
    private const int Ys = Xs + EntityTable.Capacity;
    private const int Arrays = Ys + EntityTable.Capacity;
    private const int Lives = Arrays + (9 * EntityTable.Capacity);
    private const int EnemyCount = Lives + (8 * PlayerTable.Capacity);
    private const int Undying = EnemyCount + 1;
    private const int Length = Undying + 1;

    private static readonly LevelStore s_levels = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

    private static readonly SolidMap s_map = SolidMap.Build(s_levels.Level(1), []);

    private delegate Span<byte> SpanGetter();

    public static TheoryData<int> Respawns() => Indices('R');

    public static TheoryData<int> Flashes() => Indices('S');

    [Theory]
    [MemberData(nameof(Respawns))]
    public void RespawnsExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture('R')[line];
        Assert.Equal(Length, before.Length);

        Rig rig = new(before);

        if ((sbyte)after[Lives] < 0)
        {
            Assert.Throws<NotSupportedException>(() => rig.Respawn.Update(0));
            return;
        }

        rig.Respawn.Update(0);

        Assert.Equal(Convert.ToHexString(after[States..]), Convert.ToHexString(rig.Save()[States..]));
    }

    [Theory]
    [MemberData(nameof(Flashes))]
    public void FlashesExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture('S')[line];
        Assert.Equal(Length, before.Length);

        Rig rig = new(before);

        Frame(rig.Entities).Slot(before[Slot], Input.Idle, before[Counter], s_map);

        Assert.Equal(Convert.ToHexString(after[States..Lives]), Convert.ToHexString(rig.Save()[States..Lives]));
    }

    [Fact]
    public void CoversEveryLifeToTheGameOver()
    {
        Assert.Equal(4, Capture('R').Count);
        Assert.Equal(3 * 0x4A, Capture('S').Count);
        Assert.Equal(3, Capture('S').Count(c => c.After[States + c.Before[Slot]] == PlayerFrame.PlayingState));
    }

    private static TheoryData<int> Indices(char kind)
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture(kind).Count; i++)
        {
            lines.Add(i);
        }

        return lines;
    }

    private static PlayerFrame Frame(EntityTable entities)
    {
        BubbleBlow blow = TestBlow.Of(entities);
        PlayerSteer steer = new(entities, blow);
        PlayerDescent descent = new(entities);

        return new(
            entities,
            new PlayerMovement(entities, blow, TestBlow.NoRings()),
            new PlayerJump(entities, new PlayerDrift(entities, steer, TestBlow.NoRings()), new PlayerLanding(entities, descent)),
            new PlayerFall(entities, steer),
            blow);
    }

    private static List<(byte[] Before, byte[] After)> Capture(char kind)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Players", "Captures", "respawn-level-1.txt");

        return [.. File.ReadAllLines(path)
            .Where(line => line[0] == kind)
            .Select(line =>
            {
                string[] parts = line.Split(' ');
                return (Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
            })];
    }

    private sealed class Rig
    {
        internal Rig(byte[] b)
        {
            Items = new(Entities, new Scores(), new(Players, Entities), new(new FakeRandomSource()));
            Respawn = new(Entities, Players, Items);

            b.AsSpan(States, EntityTable.Capacity).CopyTo(Entities.State);
            b.AsSpan(Xs, EntityTable.Capacity).CopyTo(Entities.X);
            b.AsSpan(Ys, EntityTable.Capacity).CopyTo(Entities.Y);

            for (int i = 0; i < EntityArrays.Length; i++)
            {
                b.AsSpan(Arrays + (i * EntityTable.Capacity), EntityTable.Capacity).CopyTo(EntityArrays[i]());
            }

            for (int i = 0; i < PlayerArrays.Length; i++)
            {
                b.AsSpan(Lives + (i * PlayerTable.Capacity), PlayerTable.Capacity).CopyTo(PlayerArrays[i]());
            }

            // $8818 is not captured. $05C5 sets it to $FF, and a player who never blows leaves it there.
            Entities.BubbleTimer.Fill(0xFF);

            Entities.EnemyCount = b[EnemyCount];
            Items.Undying = b[Undying];
        }

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal LevelItems Items { get; }

        internal PlayerRespawn Respawn { get; }

        // $8520, $8548, $8610, $8688, $86D8, $8728, $87A0, $87C8, $87F0.
        private SpanGetter[] EntityArrays => [
            () => Entities.Frame, () => Entities.Colour, () => Entities.AnimationTimer, () => Entities.TurnInterval,
            () => Entities.LeapFlag, () => Entities.FlashTimer, () => Entities.RiseCounter, () => Entities.FallCounter,
            () => Entities.GroundState,
        ];

        // $045A, $A77B, $A77D, $A77F, $A781, $61, $63, $65: two bytes each, one a player.
        private SpanGetter[] PlayerArrays => [
            () => Players.Lives, () => Players.BlowReload, () => Players.BlowState, () => Players.BlowVariant,
            () => Players.BlowType, () => Players.DriftRing, () => Players.WalkRing, () => Players.BlowRing,
        ];

        internal byte[] Save()
        {
            byte[] b = new byte[Length];
            Entities.State.CopyTo(b.AsSpan(States));
            Entities.X.CopyTo(b.AsSpan(Xs));
            Entities.Y.CopyTo(b.AsSpan(Ys));

            for (int i = 0; i < EntityArrays.Length; i++)
            {
                EntityArrays[i]().CopyTo(b.AsSpan(Arrays + (i * EntityTable.Capacity)));
            }

            for (int i = 0; i < PlayerArrays.Length; i++)
            {
                PlayerArrays[i]().CopyTo(b.AsSpan(Lives + (i * PlayerTable.Capacity)));
            }

            b[EnemyCount] = Entities.EnemyCount;
            b[Undying] = Items.Undying;

            return b;
        }
    }
}
