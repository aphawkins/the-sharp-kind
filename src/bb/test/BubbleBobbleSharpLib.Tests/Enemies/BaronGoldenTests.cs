// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Enemies;

// The Baron against the real thing, a pass at a time.
//
// Captures/baron-level-1.txt was read out of VICE running rebb64, on 2026-09-24, on level 1 with one
// player. The six enemy slots $B4 to $B9 were zeroed as the level started, which is an intervention,
// so that the player lived until the hurry-up ran out. Once $CA held $2E, two exec checkpoints
// bracketed him: $1473, his entry, and the next $0D4A, where every arm of his leaves. The player was
// moved by a key every two and a half seconds.
//
// Each line is one pass: the bytes before, then after. $B2, $B3, $BA, $BB, $C2, $C3, $CA, $CB, $DC,
// $DD, $EE, $EF, $AA30, $AA31, $AA42, $AA43, $A775, $A776, $E765 and $E766, then $1545 and $1549 -
// $152B's two compare operands, which $1496 changes and puts back - then $2B and $67.
[Trait("Level", "Integration")]
public sealed class BaronGoldenTests
{
    // Player 1's Baron.
    private const int Slot = 0;

    // $2B, the level timer's, which the Baron does not write here, and after it $67.
    private const int Compared = 20;
    private const int Freeze = 23;

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

        // $0D56's gate on $67 is not translated, and no pass here reaches it. $1545 and $1549 are
        // the operands $1496 restores, so they read $19 and $0D between passes.
        Assert.Equal(0, before[Freeze]);
        Assert.Equal(0x19, before[20]);
        Assert.Equal(0x0D, before[21]);

        ObjectTable objects = new();
        EntityTable entities = new();
        Baron baron = new(objects, entities, new());
        Load(objects, entities, baron, before);

        baron.Step(Slot);

        Assert.Equal(Convert.ToHexString(after, 0, Compared), Convert.ToHexString(Save(objects, entities, baron)));
    }

    [Fact]
    public void CoversEveryArm()
    {
        List<(byte[] Before, byte[] After)> capture = Capture();

        // A kill, a column moved, a row moved, a new run planned, a run arrived, and both facings.
        Assert.Contains(capture, c => c.Before[0] != 0x0E && c.After[0] == 0x0E);
        Assert.Contains(capture, c => c.Before[8] != c.After[8]);
        Assert.Contains(capture, c => c.Before[10] != c.After[10]);
        Assert.Contains(capture, c => (c.Before[14] & 0x7F) == 0);
        Assert.Contains(capture, c => c.Before[14] != 0 && c.After[14] == 0);
        Assert.Contains(capture, c => c.After[18] == 0x00);
        Assert.Contains(capture, c => c.After[18] == 0x02);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Enemies", "Captures", "baron-level-1.txt");

        return [.. File.ReadAllLines(path).Select(line =>
        {
            string[] parts = line.Split(' ');
            return (Convert.FromHexString(parts[0]), Convert.FromHexString(parts[1]));
        })];
    }

    private static void Load(ObjectTable objects, EntityTable entities, Baron baron, byte[] b)
    {
        for (int i = 0; i < 2; i++)
        {
            entities.State[i] = b[i];
            entities.X[i] = b[2 + i];
            entities.Y[i] = b[4 + i];
            objects.Type[i] = b[6 + i];
            objects.Column[i] = b[8 + i];
            objects.Row[i] = b[10 + i];
            objects.Variant[i] = b[12 + i];
            objects.EnemyType[i] = b[14 + i];
            baron.Waiting[i] = b[16 + i];
            baron.Facing[i] = b[18 + i];
        }
    }

    private static byte[] Save(ObjectTable objects, EntityTable entities, Baron baron) => [
        entities.State[0], entities.State[1], entities.X[0], entities.X[1], entities.Y[0], entities.Y[1],
        objects.Type[0], objects.Type[1], objects.Column[0], objects.Column[1], objects.Row[0], objects.Row[1],
        objects.Variant[0], objects.Variant[1], objects.EnemyType[0], objects.EnemyType[1],
        baron.Waiting[0], baron.Waiting[1], baron.Facing[0], baron.Facing[1],
    ];
}
