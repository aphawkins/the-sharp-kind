// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// A respawn or a game over restarting the level's clock ($0504-$0512), against the real thing.
//
// Captures/restart-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1. Two exec
// checkpoints bracket $045C: its entry and the rts of $052A ($05AC). Ten calls.
//
// INTERVENTIONS. Before each call a player was made dead ($0F) with two lives or none, and $2A, $2B and
// $2D were written: an ordinary clock, one with the hurry-up begun ($2D of 1, 5 and $7F), one stopped
// ($2B of $FF), and one with the level cleared ($2D of $80 and $FF).
//
// Each line is the bytes before and after: $2A-$2D, $B2-$B3, $045A-$045B.
[Trait("Level", "Integration")]
public sealed class LevelTimerRestartGoldenTests
{
    private const int Seconds = 0;
    private const int Frames = 1;
    private const int Start = 2;
    private const int Hurry = 3;
    private const int States = 4;
    private const int Lives = 6;
    private const int Length = 8;

    public static TheoryData<int> Calls()
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture().Count; i++)
        {
            lines.Add(i);
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Calls))]
    public void RestartsTheClockExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];
        Assert.Equal(Length, before.Length);

        Rig rig = new(before);

        rig.Respawn.Update(0);

        Assert.Equal(Convert.ToHexString(after), Convert.ToHexString(rig.Save()));
    }

    [Fact]
    public void CoversRestartsAndTheTwoWaysToBeLeftAlone()
    {
        List<(byte[] Before, byte[] After)> calls = Capture();

        Assert.Equal(10, calls.Count);
        Assert.Contains(calls, c => c.After[Seconds] != c.Before[Seconds]);
        Assert.Contains(calls, c => c.After[Seconds] == c.Before[Seconds] && c.Before[Frames] == LevelTimer.Stopped);
        Assert.Contains(calls, c => c.After[Seconds] == c.Before[Seconds] && (sbyte)c.Before[Hurry] < 0);
        Assert.Contains(calls, c => c.After[Seconds] != c.Before[Seconds] && (sbyte)c.After[Lives] < 0);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Levels", "Captures", "restart-level-1.txt");

        return [.. File.ReadAllLines(path)
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
            Respawn = new(Entities, Players, Items, Timer);

            Timer.Begin(0);
            Timer.Seconds = b[Seconds];
            Timer.Frames = b[Frames];
            Timer.Hurry = b[Hurry];

            b.AsSpan(States, PlayerTable.Capacity).CopyTo(Entities.State);
            b.AsSpan(Lives, PlayerTable.Capacity).CopyTo(Players.Lives);
        }

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal LevelItems Items { get; }

        internal LevelTimer Timer { get; } = new();

        internal PlayerRespawn Respawn { get; }

        internal byte[] Save()
        {
            byte[] b = new byte[Length];

            b[Seconds] = Timer.Seconds;
            b[Frames] = Timer.Frames;
            b[Start] = Timer.Start;
            b[Hurry] = Timer.Hurry;
            Entities.State[..PlayerTable.Capacity].CopyTo(b.AsSpan(States));
            Players.Lives.CopyTo(b.AsSpan(Lives));

            return b;
        }
    }
}
