// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// A dying player against the real thing, a pass at a time.
//
// Captures/dying-level-1.txt was read out of VICE running rebb64, on 2026-09-26, on level 1 with one
// player walking left and right and jumping until the game was over. No intervention. Two exec
// checkpoints bracket one player slot's turn in $1CBD, both with the condition X < 2: $1CDB, the loop
// head, and $1D24, where the loop moves to the next slot. That takes in $1D03's extra call as well
// as the ordinary one. Only turns where the slot was dying or dead are kept: four deaths, 88 turns
// each, the last of each moving the state to $0F.
//
// Each line is the bytes before and after: $08, $22 (the slot), $B2 to $C9, then $8520, $8610, $8638,
// $86D8, $8728, $87A0 and $87F0 for all eight slots. No death in the capture had $8728 set, so the
// extra call is proved by hand only, in PlayerDyingTests.
[Trait("Level", "Integration")]
public sealed class PlayerDyingGoldenTests
{
    private const int Counter = 0;
    private const int Slot = 1;
    private const int States = 2;
    private const int Xs = States + EntityTable.Capacity;
    private const int Ys = Xs + EntityTable.Capacity;
    private const int Arrays = Ys + EntityTable.Capacity;
    private const int Length = Arrays + (7 * EntityTable.Capacity);

    private static readonly SolidMap s_map = SolidMap.Build(
        new() { Number = 1, Bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)] },
        []);

    public static TheoryData<int> Lines()
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture().Count; i++)
        {
            lines.Add(i);
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Lines))]
    public void DiesExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];
        Assert.Equal(Length, before.Length);

        EntityTable entities = new();
        Load(before, entities);

        Frame(entities).Slot(before[Slot], Input.Idle, before[Counter], s_map);

        byte[] expected = after[States..];
        byte[] actual = Save(entities)[States..];
        Assert.Equal(Convert.ToHexString(expected), Convert.ToHexString(actual));
    }

    [Fact]
    public void CoversEveryDeathToTheEnd()
    {
        List<(byte[] Before, byte[] After)> capture = Capture();

        Assert.Equal(4, capture.Count(c => c.Before[Arrays + (3 * EntityTable.Capacity)] == 0));
        Assert.Equal(4, capture.Count(c => c.After[States + c.Before[Slot]] == PlayerFrame.DeadState));
    }

    private static Span<byte> Array(byte[] b, int index) => b.AsSpan(Arrays + (index * EntityTable.Capacity), EntityTable.Capacity);

    private static void Load(byte[] b, EntityTable entities)
    {
        b.AsSpan(States, EntityTable.Capacity).CopyTo(entities.State);
        b.AsSpan(Xs, EntityTable.Capacity).CopyTo(entities.X);
        b.AsSpan(Ys, EntityTable.Capacity).CopyTo(entities.Y);
        Array(b, 0).CopyTo(entities.Frame);
        Array(b, 1).CopyTo(entities.AnimationTimer);
        Array(b, 2).CopyTo(entities.HoldTimer);
        Array(b, 3).CopyTo(entities.LeapFlag);
        Array(b, 4).CopyTo(entities.FlashTimer);
        Array(b, 5).CopyTo(entities.RiseCounter);
        Array(b, 6).CopyTo(entities.GroundState);
    }

    private static byte[] Save(EntityTable entities)
    {
        byte[] b = new byte[Length];
        entities.State.CopyTo(b.AsSpan(States));
        entities.X.CopyTo(b.AsSpan(Xs));
        entities.Y.CopyTo(b.AsSpan(Ys));
        entities.Frame.CopyTo(Array(b, 0));
        entities.AnimationTimer.CopyTo(Array(b, 1));
        entities.HoldTimer.CopyTo(Array(b, 2));
        entities.LeapFlag.CopyTo(Array(b, 3));
        entities.FlashTimer.CopyTo(Array(b, 4));
        entities.RiseCounter.CopyTo(Array(b, 5));
        entities.GroundState.CopyTo(Array(b, 6));

        return b;
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

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Players", "Captures", "dying-level-1.txt");

        return [.. File.ReadAllLines(path).Select(line =>
        {
            string[] parts = line.Split(' ');
            return (Convert.FromHexString(parts[0]), Convert.FromHexString(parts[1]));
        })];
    }
}
