// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The clock running out on a level not yet cleared, against the real thing.
//
// Captures/hurry-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1. Four exec
// checkpoints: $15E1, where the clock is tested, $1693, the rts the hurry-up and the Barons' spawn both end in,
// and $1643 and $1650, where the four rolls of the spawn return (their A is the fourth field).
//
// INTERVENTIONS. At $15E1 the clock ($2A) was written 0 and $2B $20, $2D the hurry-up state (0 for the first
// expiry, 1, 2 or 5 for the second), the players' states, the six enemies' states and flashes, and the two
// object slots' types, columns, rows and the arrays the Baron keeps. Afterwards the clock, $2D and the players
// were put back. A ninth case, with a type $18 in slot 0, hung the machine and is left out.
//
// Each line is the bytes before and after, then the rolls. A side is: $2A-$2D, $58, $58FF, $B2-$B3, $B4-$B9,
// $872A-$872F, then eighteen objects' types ($CA), columns ($DC), rows ($EE), $A9C4, $A9D6, $AA30 and $AA42, and
// $26-$27. The hurry-up runs the HURRY UP! scroll, in which the clock goes on, so its $2B is not compared.
[Trait("Level", "Integration")]
public sealed class LevelHurryGoldenTests
{
    private const int Objects = ObjectTable.Capacity;

    private const int Seconds = 0;
    private const int Frames = 1;
    private const int Hurry = 3;
    private const int Seed = 4;
    private const int Players = 6;
    private const int Enemies = 8;
    private const int Flashes = 14;
    private const int Types = 20;
    private const int Columns = Types + Objects;
    private const int Rows = Columns + Objects;
    private const int SubX = Rows + Objects;
    private const int SubY = SubX + Objects;
    private const int Variants = SubY + Objects;
    private const int EnemyTypes = Variants + Objects;
    private const int RandomAt = EnemyTypes + Objects;
    private const int Length = RandomAt + 2;

    public static TheoryData<int> Expiries()
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture().Count; i++)
        {
            lines.Add(i);
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Expiries))]
    public void RunsOutExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after, byte[] rolls) = Capture()[line];
        Assert.Equal(Length, before.Length);
        Assert.Equal(Length, after.Length);

        EndRig rig = new(ScriptedRandomSource.For(before[RandomAt], before[RandomAt + 1], rolls));
        Load(rig, before);

        rig.End.Update();

        Assert.Equal(after[Seconds], rig.Timer.Seconds);
        Assert.Equal(after[Hurry], rig.Timer.Hurry);
        Assert.Equal(after[Seed], rig.Items.FoodBase);
        Assert.Equal(Convert.ToHexString(after.AsSpan(Flashes, 6)), Convert.ToHexString(rig.Entities.FlashTimer[2..]));
        Assert.Equal(Convert.ToHexString(after.AsSpan(Enemies, 6)), Convert.ToHexString(rig.Entities.State[2..]));
        Assert.Equal(Convert.ToHexString(after.AsSpan(Types, Objects)), Convert.ToHexString(rig.Objects.Type));
        Assert.Equal(Convert.ToHexString(after.AsSpan(Columns, Objects)), Convert.ToHexString(rig.Objects.Column));
        Assert.Equal(Convert.ToHexString(after.AsSpan(Rows, Objects)), Convert.ToHexString(rig.Objects.Row));
        Assert.Equal(Convert.ToHexString(after.AsSpan(SubX, Objects)), Convert.ToHexString(rig.Objects.SubX));
        Assert.Equal(Convert.ToHexString(after.AsSpan(SubY, Objects)), Convert.ToHexString(rig.Objects.SubY));
        Assert.Equal(Convert.ToHexString(after.AsSpan(Variants, Objects)), Convert.ToHexString(rig.Objects.Variant));
        Assert.Equal(Convert.ToHexString(after.AsSpan(EnemyTypes, Objects)), Convert.ToHexString(rig.Objects.EnemyType));
        Assert.Equal(after[RandomAt], rig.Random.Low);
        Assert.Equal(after[RandomAt + 1], rig.Random.High);

        if (before[Hurry] != 0)
        {
            Assert.Equal(after[Frames], rig.Timer.Frames);
        }
    }

    [Fact]
    public void CoversTheHurryUpAndSeveralSpawns()
    {
        List<(byte[] Before, byte[] After, byte[] Rolls)> expiries = Capture();

        Assert.Equal(8, expiries.Count);
        Assert.Equal(3, expiries.Count(e => e.Before[Hurry] == 0));
        Assert.Equal(5, expiries.Count(e => e.Before[Hurry] != 0));
        Assert.All(expiries.Where(e => e.Before[Hurry] != 0), e => Assert.Equal(4, e.Rolls.Length));
        Assert.Contains(expiries, e => e.After[Types] == 0x2E && e.After[Types + 1] == 0x30);
        Assert.Contains(expiries, e => e.After[Types] == 0xFF && e.After[Types + 1] == 0x30);
    }

    private static void Load(EndRig rig, byte[] b)
    {
        rig.Timer.Seconds = b[Seconds];
        rig.Timer.Frames = b[Frames];
        rig.Timer.Hurry = b[Hurry];
        rig.Items.FoodBase = b[Seed];
        rig.Random.Low = b[RandomAt];
        rig.Random.High = b[RandomAt + 1];
        b.AsSpan(Players, PlayerTable.Capacity).CopyTo(rig.Entities.State);
        b.AsSpan(Enemies, 6).CopyTo(rig.Entities.State[2..]);
        b.AsSpan(Flashes, 6).CopyTo(rig.Entities.FlashTimer[2..]);
        b.AsSpan(Types, Objects).CopyTo(rig.Objects.Type);
        b.AsSpan(Columns, Objects).CopyTo(rig.Objects.Column);
        b.AsSpan(Rows, Objects).CopyTo(rig.Objects.Row);
        b.AsSpan(SubX, Objects).CopyTo(rig.Objects.SubX);
        b.AsSpan(SubY, Objects).CopyTo(rig.Objects.SubY);
        b.AsSpan(Variants, Objects).CopyTo(rig.Objects.Variant);
        b.AsSpan(EnemyTypes, Objects).CopyTo(rig.Objects.EnemyType);
        rig.Entities.EnemyCount = 3;
    }

    private static List<(byte[] Before, byte[] After, byte[] Rolls)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Levels", "Captures", "hurry-level-1.txt");

        return [.. File.ReadAllLines(path)
            .Select(line =>
            {
                string[] parts = line.Split(' ');
                return (
                    Convert.FromHexString(parts[1]),
                    Convert.FromHexString(parts[2]),
                    parts.Length > 3 ? Convert.FromHexString(parts[3]) : []);
            })];
    }
}
