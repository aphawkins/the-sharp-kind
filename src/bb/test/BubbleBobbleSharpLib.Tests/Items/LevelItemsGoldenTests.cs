// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Items;

// The level's two items as $2B31 sets them up, against the real thing, for levels 1 to 99.
//
// Captures/items-setup.txt was read out of VICE running rebb64, on 2026-09-24, with one player. The
// game stopped at $09F1, the `jsr $2B31` as level 1 starts. SUBFLG was written with each level in
// turn and PC put back to $09F1 after each call, so every level after the first is an intervention,
// and each call starts from the RNG and the milestone the one before left.
//
// Each line is the bytes before and after - SUBFLG, $26, $27, $58, $52 to $5E, $5B3F and $5B7F -
// then every call to $E9EA: $26 and $27 on the way in and $26 on the way out. That gives each draw's
// CIA byte exactly, so every draw is matched rather than searched for.
[Trait("Level", "Integration")]
public sealed class LevelItemsGoldenTests
{
    private const int Number = 0;
    private const int RandomLow = 1;
    private const int RandomHigh = 2;
    private const int FoodBase = 3;
    private const int Types = 4;
    private const int Positions = 11;
    private const int Milestone = 17;
    private const int Undying = 18;

    private static readonly LevelStore s_store = LevelStore.Read(Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        "levels.json"));

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
    public void SetsUpExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after, byte[][] draws) = Capture()[line];

        BbRandom random = new(new Replay(draws))
        {
            Low = before[RandomLow],
            High = before[RandomHigh],
        };

        EntityTable entities = new();
        LevelItems items = new(entities, new(), new(new(), entities), random)
        {
            FoodBase = before[FoodBase],
            Milestone = before[Milestone],
            Undying = before[Undying],
        };

        items.Setup(s_store.Level(before[Number] + 1), before[Number]);

        byte[] expected =
        [
            after[RandomLow], after[RandomHigh], after[FoodBase], after[Types], after[Types + 1],
            .. after.AsSpan(Positions, 6), after[Milestone],
        ];

        byte[] actual =
        [
            random.Low, random.High, items.FoodBase, items.Type[0], items.Type[1],
            items.X[0], items.X[1], items.Y[0], items.Y[1], items.Timer[0], items.Timer[1], items.Milestone,
        ];

        Assert.Equal(Convert.ToHexString(expected), Convert.ToHexString(actual));
    }

    // The three ways to the special item, and a capped food.
    [Fact]
    public void CoversEveryArm()
    {
        List<(byte[] Before, byte[] After, byte[][] Draws)> capture = Capture();

        Assert.Contains(capture, c => c.After[Milestone] != c.Before[Milestone]);
        Assert.Contains(capture, c => c.Draws.Length == 6);
        Assert.Contains(capture, c => c.Draws.Length == 5);
        Assert.Contains(capture, c => (c.After[Types + 1] & 0x7E) == 0x1E && c.After[Milestone] == c.Before[Milestone]);
        Assert.Equal(99, capture.Count);
    }

    private static List<(byte[] Before, byte[] After, byte[][] Draws)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Items", "Captures", "items-setup.txt");

        return [.. File.ReadAllLines(path).Select(Parse)];
    }

    private static (byte[] Before, byte[] After, byte[][] Draws) Parse(string line)
    {
        string[] parts = line.Split(' ');
        return (Convert.FromHexString(parts[0]), Convert.FromHexString(parts[1]), [.. parts[2].Split(',').Select(Convert.FromHexString)]);
    }

    // Each draw's CIA byte: what $E9EA left in $26, against its own shift and fold of what it found.
    private sealed class Replay(byte[][] draws) : IRandomSource
    {
        private readonly Queue<int> _bytes = new(draws.Select(Timer));

        public int NextInt() => throw new NotSupportedException();

        public int Random(int toExclusive) => _bytes.Dequeue();

        public int Random(int fromInclusive, int toExclusive) => throw new NotSupportedException();

        public bool TrueOrFalse() => throw new NotSupportedException();

        public int GaussianRandom(int min, int max) => throw new NotSupportedException();

        private static int Timer(byte[] draw)
        {
            BbRandom bare = new(new FakeRandomSource()) { Low = draw[0], High = draw[1] };
            return bare.Next() ^ draw[2];
        }
    }
}
