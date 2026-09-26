// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $1D32 against the real thing, a call at a time.
//
// Captures/death-level-1.txt was read out of VICE running rebb64, on 2026-09-26, on level 1 with one
// player walking left and right and jumping, until the game was over. No intervention. Two exec
// checkpoints bracket the routine: $1D32 itself and $1D7C, its `rts`. It runs inside the IRQ, so
// nothing can run in the middle of it.
//
// Each line is the bytes before and after: $B2 to $C9 (state, X and Y for all eight slots), then
// $5AFF, which was zero in every call - see PlayerDeath. There are four deaths among 1,232 calls,
// and 42 enemies within twenty pixels of a playing player.
[Trait("Level", "Integration")]
public sealed class PlayerDeathGoldenTests
{
    private const int States = 0;
    private const int Xs = 8;
    private const int Ys = 16;
    private const int Flag = 24;
    private const int Length = 25;

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
    public void KillsExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];
        Assert.Equal(Length, before.Length);
        Assert.Equal(0, before[Flag]);

        EntityTable entities = new();

        for (int slot = 0; slot < EntityTable.Capacity; slot++)
        {
            entities.State[slot] = before[States + slot];
            entities.X[slot] = before[Xs + slot];
            entities.Y[slot] = before[Ys + slot];
        }

        new PlayerDeath(entities).Update();

        byte[] actual = [.. entities.State, .. entities.X, .. entities.Y, before[Flag]];
        Assert.Equal(Convert.ToHexString(after), Convert.ToHexString(actual));
    }

    [Fact]
    public void CoversADeath()
        => Assert.Contains(Capture(), c => c.Before[States] == 0x01 && c.After[States] == 0x0E);

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Players", "Captures", "death-level-1.txt");

        return [.. File.ReadAllLines(path).Select(line =>
        {
            string[] parts = line.Split(' ');
            return (Convert.FromHexString(parts[0]), Convert.FromHexString(parts[1]));
        })];
    }
}
