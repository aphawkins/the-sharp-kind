// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Players;
using SharpKind;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// The pop against the real thing, a renderer pass at a time.
//
// Captures/pop-level-1.txt was read out of VICE running rebb64, on 2026-09-24, on level 1 with one
// player. Every scene is an intervention: at an exec checkpoint on $E90E the eighteen object slots
// and the six enemies were cleared and a scene was written in, and then eight passes were bracketed
// by $E90E and whichever of $E9B4 and $E9B7 came next. The scenes are a touched bubble with four
// around it, one of them wobbling; a caught enemy with a second caught one beside it; a letter
// bubble; and a bubble whose time ran out.
//
// Each line is the scene, then the bytes before and after: $0000 to $00FF, $0193 to $01A4, $0400 to
// $0405, $8520 to $88B7 and $A9B2 to $AA53.
//
// A kill draws three random numbers. The CIA bytes behind them are searched for here: each draw's
// low bits are in what the kill kept, and the last draw is $26 itself.
[Trait("Level", "Integration")]
public sealed class BubblePopGoldenTests
{
    private const int Zero = 0;
    private const int Direction = 256;
    private const int Score = 274;
    private const int Region = 280;
    private const int Objects = 1200;

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
    public void StepsExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];

        ObjectTable objects = new();
        EntityTable entities = new();
        PlayerTable players = new();
        Scores scores = new();
        BbRandom random = new(new Replay(Timers(before, after)))
        {
            Low = before[Zero + 0x26],
            High = before[Zero + 0x27],
        };

        BubblePop pop = new(objects, entities, players, new(entities, scores, random), scores);
        Load(before, objects, entities, players, scores, pop);

        pop.Update();

        Assert.Equal(Convert.ToHexString(Pick(after)), Convert.ToHexString(Save(objects, entities, players, scores, pop, random)));
    }

    // Both chain directions, a release, the chain's score, a letter, the expiry and every frame.
    [Fact]
    public void CoversEveryArm()
    {
        List<(byte[] Before, byte[] After)> capture = Capture();

        Assert.Contains(capture, c => c.Before[Zero + 0xCA + 8] == 0x04 && c.After[Zero + 0xCA + 8] == 0x3C);
        Assert.Contains(capture, c => c.Before[Zero + 0xCA + 12] == 0x04 && c.After[Zero + 0xCA + 12] == 0x34);
        Assert.Contains(capture, c => c.Before[Zero + 0xB6] == 0 && c.After[Zero + 0xB6] == 0x0B);
        Assert.Contains(capture, c => c.After[Score + 1] == 0x02);
        Assert.Contains(capture, c => c.Before[Zero + 0x54] == 0 && c.After[Zero + 0x54] != 0);
        Assert.Contains(capture, c => c.Before[Zero + 0xCA + 10] == 0x3A);

        foreach (int type in new[] { 0x3C, 0x3E, 0x40, 0x38, 0x36 })
        {
            Assert.Contains(capture, c => c.Before[Zero + 0xCA + 10] == type);
        }
    }

    private static void Load(byte[] b, ObjectTable objects, EntityTable entities, PlayerTable players, Scores scores, BubblePop pop)
    {
        for (int i = 0; i < ObjectTable.Capacity; i++)
        {
            objects.Type[i] = b[Zero + 0xCA + i];
            objects.Column[i] = b[Zero + 0xDC + i];
            objects.Row[i] = b[Zero + 0xEE + i];
            objects.Direction[i] = b[Direction + i];
            objects.State[i] = b[Objects + i];
            objects.X[i] = b[Objects + 0x5A + i];
            objects.Y[i] = b[Objects + 0x6C + i];
            objects.Variant[i] = b[Objects + 0x7E + i];
            objects.EnemyType[i] = b[Objects + 0x90 + i];
        }

        for (int i = 0; i < EntityTable.Capacity; i++)
        {
            entities.State[i] = b[Zero + 0xB2 + i];
            entities.Colour[i] = b[At(0x8548, i)];
            entities.RiseCounter[i] = b[At(0x87A0, i)];
            entities.FallCounter[i] = b[At(0x87C8, i)];
            entities.GroundState[i] = b[At(0x87F0, i)];
            entities.BubbleTimer[i] = b[At(0x8818, i)];
            entities.LeftFlag[i] = b[At(0x8840, i)];
            entities.RightFlag[i] = b[At(0x8868, i)];
            entities.AttackTimer[i] = b[At(0x8890, i)];
        }

        entities.EnemyCount = b[Zero + 0x4A];

        for (int p = 0; p < PlayerTable.Capacity; p++)
        {
            players.ExtendLetters[p] = b[Zero + 0x54 + p];
            pop.Chain[p] = b[Zero + 0x46 + p];
        }

        for (int i = 0; i < 6; i++)
        {
            scores.Add(i, b[Score + i]);
        }
    }

    // What the pop can change, read out of a capture's bytes and out of the port in the same order.
    private static byte[] Pick(byte[] b)
    {
        List<byte> out1 = [];

        for (int i = 0; i < ObjectTable.Capacity; i++)
        {
            out1.AddRange([b[Zero + 0xCA + i], b[Zero + 0xDC + i], b[Zero + 0xEE + i], b[Direction + i], b[Objects + 0x90 + i]]);
        }

        for (int i = 0; i < EntityTable.Capacity; i++)
        {
            out1.AddRange(
            [
                b[Zero + 0xB2 + i], b[At(0x8548, i)], b[At(0x87A0, i)], b[At(0x87C8, i)], b[At(0x87F0, i)],
                b[At(0x8818, i)], b[At(0x8840, i)], b[At(0x8868, i)], b[At(0x8890, i)],
            ]);
        }

        out1.AddRange([b[Zero + 0x4A], b[Zero + 0x54], b[Zero + 0x55], b[Zero + 0x46], b[Zero + 0x47], b[Zero + 0x26], b[Zero + 0x27]]);
        out1.AddRange(b.AsSpan(Score, 6).ToArray());

        return [.. out1];
    }

    private static byte[] Save(
        ObjectTable objects, EntityTable entities, PlayerTable players, Scores scores, BubblePop pop, BbRandom random)
    {
        List<byte> out1 = [];

        for (int i = 0; i < ObjectTable.Capacity; i++)
        {
            out1.AddRange([objects.Type[i], objects.Column[i], objects.Row[i], objects.Direction[i], objects.EnemyType[i]]);
        }

        for (int i = 0; i < EntityTable.Capacity; i++)
        {
            out1.AddRange(
            [
                entities.State[i], entities.Colour[i], entities.RiseCounter[i], entities.FallCounter[i],
                entities.GroundState[i], entities.BubbleTimer[i], entities.LeftFlag[i], entities.RightFlag[i],
                entities.AttackTimer[i],
            ]);
        }

        out1.AddRange([entities.EnemyCount, players.ExtendLetters[0], players.ExtendLetters[1]]);
        out1.AddRange([pop.Chain[0], pop.Chain[1], random.Low, random.High]);
        out1.AddRange(scores.Bytes.ToArray());

        return [.. out1];
    }

    private static int At(int address, int slot) => Region + address - 0x8520 + slot;

    // The CIA bytes behind a kill's three draws: the first two chosen by the low bits $1A6F kept,
    // the third by $26 as it was left, and the whole checked against $27.
    private static int[] Timers(byte[] before, byte[] after)
    {
        byte low = before[Zero + 0x26];
        byte high = before[Zero + 0x27];

        if (low == after[Zero + 0x26] && high == after[Zero + 0x27])
        {
            return [];
        }

        int enemy = Enumerable.Range(2, 6).Single(i => before[Zero + 0xB2 + i] != 0x0B && after[Zero + 0xB2 + i] == 0x0B);
        byte rise = after[At(0x87A0, enemy)];
        byte fall = after[At(0x87C8, enemy)];
        byte ground = after[At(0x87F0, enemy)];

        for (int first = 0; first < 0x100; first++)
        {
            for (int second = 0; second < 0x100; second++)
            {
                BbRandom trial = new(new Replay([first, second, 0])) { Low = low, High = high };

                if ((trial.Next() & 0x07) + 1 != rise || (trial.Next() & 0x07) + 1 != fall)
                {
                    continue;
                }

                BbRandom last = new(new Replay([0])) { Low = trial.Low, High = trial.High };
                int third = last.Next() ^ after[Zero + 0x26];

                if (last.High == after[Zero + 0x27] && (after[Zero + 0x26] & 0x01) == ground)
                {
                    return [first, second, third];
                }
            }
        }

        Assert.Fail("no timer bytes give the kill's draws");
        return [];
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Bubbles", "Captures", "pop-level-1.txt");

        return [.. File.ReadAllLines(path).Select(Parse)];
    }

    private static (byte[] Before, byte[] After) Parse(string line)
    {
        string[] parts = line.Split(' ');
        return (Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
    }

    private sealed class Replay(int[] bytes) : IRandomSource
    {
        private readonly Queue<int> _bytes = new(bytes);

        public int NextInt() => throw new NotSupportedException();

        public int Random(int toExclusive) => _bytes.Dequeue();

        public int Random(int fromInclusive, int toExclusive) => throw new NotSupportedException();

        public bool TrueOrFalse() => throw new NotSupportedException();

        public int GaussianRandom(int min, int max) => throw new NotSupportedException();
    }
}
