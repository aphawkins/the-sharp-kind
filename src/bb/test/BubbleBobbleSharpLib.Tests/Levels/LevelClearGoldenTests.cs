// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Items;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using SharpKind.Fakes;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Levels;

// The level clearing against the real thing.
//
// Captures/clear-level-1.txt was read out of VICE running rebb64, on 2026-09-28, on level 1. Four exec
// checkpoints: $16DA, where the clearing check is reached, $16F7, where the enemy count is tested, and its two
// ends, the rts at $1772 (C lines) and the jump into the bonus round at $3517 (the B line).
//
// INTERVENTIONS. At $16DA the enemy count ($4A) was written 0, so the level was cleared with enemies still on
// the screen; $2A, $2D, $53, $58, $5E, $58FF, all eighteen object types ($CA-$DB, from $04, $3A, $38, $41, $42,
// $44, $46, $4A and free), their saved types ($AA42) and behaviours ($A9E8) were written to junk; and $68 and
// $69 were written 0 (the C lines) and 9 and 0 (the B line, which leaves the routine for the bonus round).
//
// Each line is the bytes before and after. Before has the carry flag first, then $2A, $2B, $2D, $4A, $53, $58,
// $5D, $5E, $68, $69, $58FF, $B4, $C6, $C7, the types, $AA42 and $A9E8 (eighteen each) and $26-$27. After is
// the same without the carry. The carry was clear in all sixteen clears.
[Trait("Level", "Integration")]
public sealed class LevelClearGoldenTests
{
    private const int Seconds = 0;
    private const int Frames = 1;
    private const int Hurry = 2;
    private const int Enemies = 3;
    private const int Special = 4;
    private const int Seed = 5;
    private const int FoodTimer = 6;
    private const int SpecialTimer = 7;
    private const int Bonus = 8;
    private const int Others = 11;
    private const int Types = 14;
    private const int Saved = Types + ObjectTable.Capacity;
    private const int Behaviours = Saved + ObjectTable.Capacity;
    private const int RandomAt = Behaviours + ObjectTable.Capacity;
    private const int Length = RandomAt + 2;

    public static TheoryData<int> Clears()
    {
        TheoryData<int> lines = [];

        for (int i = 0; i < Capture().Count; i++)
        {
            lines.Add(i);
        }

        return lines;
    }

    [Theory]
    [MemberData(nameof(Clears))]
    public void ClearsTheLevelExactlyAsTheGameDoes(int line)
    {
        (byte[] before, byte[] after) = Capture()[line];
        Assert.Equal(Length + 1, before.Length);
        Assert.Equal(Length, after.Length);
        Assert.Equal(0, before[0] & 1);

        byte[] state = before[1..];
        Rig rig = new(state, after[RandomAt]);

        if (state[Bonus] == 0)
        {
            rig.Clear.Update();
        }
        else
        {
            Assert.Throws<NotSupportedException>(rig.Clear.Update);
        }

        // $B4, $C6 and $C7 are an enemy slot's, which the clear only sets when $69 is (it is not); $58FF, which it
        // clears, is not modelled, and the save writes the zero.
        byte[] saved = rig.Save();
        state[Others..(Others + 3)].CopyTo(saved.AsSpan(Others));

        Assert.Equal(Convert.ToHexString(after), Convert.ToHexString(saved));
    }

    [Fact]
    public void CoversBothWaysOfSeedingTheFoodAndBothSpecialItemStates()
    {
        List<(byte[] Before, byte[] After)> clears = Capture();

        Assert.Equal(17, clears.Count);
        Assert.Contains(clears, c => (sbyte)c.Before[1 + Seed] < 0);
        Assert.Contains(clears, c => (sbyte)c.Before[1 + Seed] >= 0);
        Assert.Contains(clears, c => (sbyte)c.Before[1 + Special] < 0);
        Assert.Contains(clears, c => (sbyte)c.Before[1 + Special] >= 0);
        Assert.Contains(clears, c => c.Before[1 + Bonus] != 0);
    }

    private static List<(byte[] Before, byte[] After)> Capture()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Levels", "Captures", "clear-level-1.txt");

        return [.. File.ReadAllLines(path)
            .Select(line =>
            {
                string[] parts = line.Split(' ');
                return (Convert.FromHexString(parts[1]), Convert.FromHexString(parts[2]));
            })];
    }

    private sealed class Rig
    {
        private readonly BbRandom _random;

        internal Rig(byte[] b, byte randomAfter)
        {
            // $E9EA mixes in a CIA timer byte. Only a clear that seeds the food asks for one: find the byte
            // that leaves the low byte where the game left it.
            _random = (sbyte)b[Seed] < 0
                ? Roll(b, randomAfter)
                : new(new FakeRandomSource()) { Low = b[RandomAt], High = b[RandomAt + 1] };
            Items = new(Entities, new Scores(), new(Players, Entities), _random);
            Clear = new(Timer, Entities, Objects, Items, _random);

            Timer.Begin(0);
            Timer.Seconds = b[Seconds];
            Timer.Frames = b[Frames];
            Timer.Hurry = b[Hurry];
            Entities.EnemyCount = b[Enemies];
            Items.Type[1] = b[Special];
            Items.FoodBase = b[Seed];
            Items.Timer[0] = b[FoodTimer];
            Items.Timer[1] = b[SpecialTimer];
            Clear.BonusType = b[Bonus];

            b.AsSpan(Types, ObjectTable.Capacity).CopyTo(Objects.Type);
            b.AsSpan(Saved, ObjectTable.Capacity).CopyTo(Objects.EnemyType);
            b.AsSpan(Behaviours, ObjectTable.Capacity).CopyTo(Objects.Behaviour);
        }

        internal LevelTimer Timer { get; } = new();

        internal EntityTable Entities { get; } = new();

        internal PlayerTable Players { get; } = new();

        internal ObjectTable Objects { get; } = new();

        internal LevelItems Items { get; }

        internal LevelClear Clear { get; }

        internal byte[] Save()
        {
            byte[] b = new byte[Length];

            b[Seconds] = Timer.Seconds;
            b[Frames] = Timer.Frames;
            b[Hurry] = Timer.Hurry;
            b[Enemies] = Entities.EnemyCount;
            b[Special] = Items.Type[1];
            b[Seed] = Items.FoodBase;
            b[FoodTimer] = Items.Timer[0];
            b[SpecialTimer] = Items.Timer[1];
            b[Bonus] = Clear.BonusType;

            Objects.Type.CopyTo(b.AsSpan(Types));
            Objects.EnemyType.CopyTo(b.AsSpan(Saved));
            Objects.Behaviour.CopyTo(b.AsSpan(Behaviours));
            b[RandomAt] = _random.Low;
            b[RandomAt + 1] = _random.High;

            return b;
        }

        private static BbRandom Roll(byte[] b, byte low)
        {
            for (int timer = 0; timer < 0x100; timer++)
            {
                BbRandom candidate = new(new FakeRandomSource { RandomValue = timer }) { Low = b[RandomAt], High = b[RandomAt + 1] };
                candidate.Next();

                if (candidate.Low == low)
                {
                    return new(new FakeRandomSource { RandomValue = timer }) { Low = b[RandomAt], High = b[RandomAt + 1] };
                }
            }

            throw new InvalidOperationException("no timer byte gives the roll the game made");
        }
    }
}
