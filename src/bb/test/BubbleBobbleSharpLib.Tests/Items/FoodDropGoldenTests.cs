// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Enemies;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Enemies;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// A killed enemy's flight, fall, landing and food against the real thing, a pass at a time.
//
// Captures/food-level-1.txt was read out of VICE running rebb64, on 2026-09-24, on level 1 with one
// player. It is four chains, and each one is an intervention: the six enemy slots were zeroed, and
// slot 2 was written as $1A6F leaves an enemy - state $0B, with the flight's speeds, class and food
// chosen here rather than rolled. The first chain's food was left to go; the other three had player
// 1's X and Y written onto the food on its twentieth pass, which is a second intervention.
//
// Two exec checkpoints bracket slot 2: $1D21, the `jsr $1E6C`, with X = 2, and the $1D24 after it.
//
// Each line is one pass, the bytes before and then after: $B2, $B3, $BA, $BB, $C2 and $C3; slot 2's
// $B4, $BC and $C4; $8522, $854A, $859A, $85EA, $872A, $87A2, $87CA, $87F2, $881A, $8842, $886A and
// $8892; $26 and $27; $0400 to $0405; then $08 and $67.
//
// The flight's turn draws one random number. Its CIA byte is worked out from $26 before and after
// and fed back in, as EnemyFrameGoldenTests does.
[Trait("Level", "Integration")]
public sealed class FoodDropGoldenTests
{
    private const int Slot = 2;

    private const int Players = 6;
    private const int State = 6;
    private const int RandomLow = 21;
    private const int RandomHigh = 22;
    private const int Score = 23;
    private const int Counter = 29;
    private const int Freeze = 30;

    private static readonly LevelStore s_store = LevelStore.Read(Path.Combine(
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

        Assert.Equal(0, before[Freeze]);

        EntityTable entities = new();
        Load(entities, before);

        Scores scores = new();

        for (int i = 0; i < 6; i++)
        {
            scores.Add(i, before[Score + i]);
        }

        BbRandom random = new(new FakeRandomSource { RandomValue = Timer(before, after) })
        {
            Low = before[RandomLow],
            High = before[RandomHigh],
        };

        EnemyFrame enemies = TestEnemies.Frame(entities, new(), random, scores);
        SolidMap map = SolidMap.Build(s_store.Level(1), s_zones.Zones(1), entities);

        enemies.Step(Slot, before[Counter], map);

        // The players' six bytes are inputs here. Nothing in slot 2's call writes them, and on two
        // lines player 1's state moved on anyway - dying to dead, and respawning - because the Baron
        // came for them while the first chain's food sat waiting.
        byte[] expected = after[..Counter];
        before.AsSpan(0, Players).CopyTo(expected);

        Assert.Equal(Convert.ToHexString(expected), Convert.ToHexString(Save(entities, random, scores)));
    }

    // Every state, a turn down, a landing, an expiry and a pickup at each kind of score.
    [Fact]
    public void CoversEveryStep()
    {
        List<(byte[] Before, byte[] After)> capture = Capture();

        foreach (byte state in new byte[] { 0x0B, 0x0C, 0x11, 0x12 })
        {
            Assert.Contains(capture, c => c.Before[State] == state);
        }

        Assert.Contains(capture, c => c.Before[15] < 0x80 && c.After[15] >= 0x80);
        Assert.Contains(capture, c => c.Before[State] == 0x0C && c.After[State] == 0x11);
        Assert.Contains(capture, c => c.Before[State] == 0x12 && c.After[State] == 0 && c.Before[14] == 1);
        Assert.Contains(capture, c => c.After[Score + 2] != c.Before[Score + 2]);
        Assert.Contains(capture, c => c.Before[20] == 0x09 && c.After[Score + 1] == 0x10);
        Assert.Contains(capture, c => c.Before[20] == 0x02 && c.After[Score + 1] != c.Before[Score + 1]);
    }

    // The CIA byte that takes $26 from before to after, if a draw was made.
    private static int Timer(byte[] before, byte[] after)
    {
        if (before[RandomLow] == after[RandomLow] && before[RandomHigh] == after[RandomHigh])
        {
            return 0;
        }

        BbRandom bare = new(new FakeRandomSource()) { Low = before[RandomLow], High = before[RandomHigh] };
        return bare.Next() ^ after[RandomLow];
    }

    private static void Load(EntityTable entities, byte[] b)
    {
        entities.State[0] = b[0];
        entities.State[1] = b[1];
        entities.X[0] = b[2];
        entities.X[1] = b[3];
        entities.Y[0] = b[4];
        entities.Y[1] = b[5];

        entities.State[Slot] = b[6];
        entities.X[Slot] = b[7];
        entities.Y[Slot] = b[8];
        entities.Frame[Slot] = b[9];
        entities.Colour[Slot] = b[10];
        entities.SpriteBase[Slot] = b[11];
        entities.Heading[Slot] = b[12];
        entities.FlashTimer[Slot] = b[13];
        entities.RiseCounter[Slot] = b[14];
        entities.FallCounter[Slot] = b[15];
        entities.GroundState[Slot] = b[16];
        entities.BubbleTimer[Slot] = b[17];
        entities.LeftFlag[Slot] = b[18];
        entities.RightFlag[Slot] = b[19];
        entities.AttackTimer[Slot] = b[20];

        // The capture wrote these, and they are what let $1CDB reach $1E6C.
        entities.Mode[Slot] = 0xFF;
    }

    private static byte[] Save(EntityTable entities, BbRandom random, Scores scores) => [
        entities.State[0], entities.State[1], entities.X[0], entities.X[1], entities.Y[0], entities.Y[1],
        entities.State[Slot], entities.X[Slot], entities.Y[Slot],
        entities.Frame[Slot], entities.Colour[Slot], entities.SpriteBase[Slot], entities.Heading[Slot],
        entities.FlashTimer[Slot], entities.RiseCounter[Slot], entities.FallCounter[Slot],
        entities.GroundState[Slot], entities.BubbleTimer[Slot], entities.LeftFlag[Slot],
        entities.RightFlag[Slot], entities.AttackTimer[Slot],
        random.Low, random.High,
        .. scores.Bytes,
    ];

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Items", "Captures", "food-level-1.txt");

        return [.. File.ReadAllLines(path).Select(Parse)];
    }

    private static (byte[] Before, byte[] After) Parse(string line)
    {
        string[] parts = line.Split(' ');
        return (Convert.FromHexString(parts[0]), Convert.FromHexString(parts[1]));
    }
}
