// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// A cleared level ending against the real thing.
//
// Captures/spawn-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1. Two exec
// checkpoints bracket $15FA-$1620, the players' return to state 1: its entry and its rts. Eleven calls.
//
// INTERVENTIONS. At $16DA the clock ($2A) was written 0 and $2D $FF, so $15E1 went to it; at its entry the
// players' states were written (the pairs 1 and 0, $10 and 0, $10 and $10, 1 and $0E, $0F and 1, 0 and $10, and
// more), with junk in their colours ($8548), flashes ($8728) and the flashes saved for them ($A813). After each
// call, $21 was written 0 and the players' states 1 and 0 again.
//
// Each line is the bytes before and after: $21, $37, $B2-$B3, $8548-$8549, $8728-$8729, $A813-$A814, $5A7F.
// $37, the pause flag ($15E4 raises it and $161E lowers it), is not modelled.
[Trait("Level", "Integration")]
public sealed class LevelEndGoldenTests
{
    private const int Complete = 0;
    private const int States = 2;
    private const int Colours = 4;
    private const int Flashes = 6;
    private const int Saved = 8;
    private const int Respawn = 10;
    private const int Length = 11;

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
    public void EndsTheLevelExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];
        Assert.Equal(Length, before.Length);
        Assert.Equal(PlayerRespawn.RespawningState, before[Respawn]);

        EndRig rig = new();
        rig.Timer.Seconds = 0;
        rig.Timer.Hurry = 0xFF;
        before.AsSpan(States, PlayerTable.Capacity).CopyTo(rig.Entities.State);
        before.AsSpan(Colours, PlayerTable.Capacity).CopyTo(rig.Entities.Colour);
        before.AsSpan(Flashes, PlayerTable.Capacity).CopyTo(rig.Entities.FlashTimer);
        before.AsSpan(Saved, PlayerTable.Capacity).CopyTo(rig.End.SavedFlash);

        rig.End.Update();

        Assert.Equal(after[Complete], rig.End.Complete);
        Assert.Equal(Convert.ToHexString(after.AsSpan(States, 2)), Convert.ToHexString(rig.Entities.State[..2]));
        Assert.Equal(Convert.ToHexString(after.AsSpan(Colours, 2)), Convert.ToHexString(rig.Entities.Colour[..2]));
        Assert.Equal(Convert.ToHexString(after.AsSpan(Flashes, 2)), Convert.ToHexString(rig.Entities.FlashTimer[..2]));
    }

    [Fact]
    public void CoversTheLevelEndingAndWaiting()
    {
        List<(byte[] Before, byte[] After)> calls = Capture();

        Assert.Equal(11, calls.Count);
        Assert.Contains(calls, c => c.After[Complete] == 1);
        Assert.Contains(calls, c => c.After[Complete] == 0);
        Assert.Contains(calls, c => c.After[Complete] == 0 && c.After[States + 1] != c.Before[States + 1]);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Levels", "Captures", "spawn-level-1.txt");

        return [.. File.ReadAllLines(path)
            .Select(line =>
            {
                string[] parts = line.Split(' ');
                return (Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
            })];
    }
}
