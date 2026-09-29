// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using BubbleBobbleSharpLib.Tests.Levels;
using BubbleBobbleSharpLib.Tests.Oracle;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Bubbles;

// $0BED and the level flags that steer it ($3A6C's tail), against the real game run on SharpKind.Emulation.
//
// The game is played from the title to level 1, then taken to each level under test with `EnterLevel` (which is not a
// play-through: it ends level 1 as `$0A6E` does, so the players carry on with what they have). No intervention is made
// beyond that: the level's own flags decide whether `$0BED` is called at all, and it is the game that patches `$0A38`.
//
// `$0BED` is bracketed from its entry to `$0A3B`, where its `jsr` returns, with interrupts off, so nothing but the routine
// touches RAM. Each call's bytes before are loaded into the port, the port runs, and the bytes after are compared: the
// objects table, the countdown at `$0BEE`, `$4C`, the seed at `$5BFF`, and the random state `$26`/`$27`. The CIA timer
// byte every `$E9EA` roll mixes in is read from the machine and replayed to the port.
[Trait("Level", "Integration")]
public sealed class BubbleSpawnerOracleTests
{
    private const int Countdown = 0x0BEE;
    private const int Chooser = 0x0C9C;
    private const int MaskOperand = 0x0CC2;
    private const int Letters = 0x4C;
    private const int SeedAddress = 0x5BFF;
    private const int SpawnSwitch = 0x0A38;
    private const int EnemyCount = 0x4A;
    private const int Random = 0x26;
    private const int JsrOpcode = 0x20;
    private const int JmpOpcode = 0x4C;

    private const int CallReturn = 0x0A3B;
    private const int TailEnd = 0x3AB7;
    private const int TailStart = 0x3A6C;

    private const int Calls = 150;

    private static readonly LevelStore s_levels = LevelStore.Read(Asset("levels.json"));
    private static readonly ZoneStore s_zones = ZoneStore.Read(Asset("zones.json"));

    // A spread of spawn flags: the mask operand takes 0 to 6, and a few levels take the letter chooser. Level 73 is left
    // out: the port's map has a bubble current in its bottom row's openings that the game's does not
    // (SolidMapOracleTests).
    public static TheoryData<int> Levels() => [3, 5, 17, 25, 68, 78, 80, 88, 99];

    [Theory]
    [MemberData(nameof(Levels))]
    public void SpawnsExactlyAsTheGameDoes(int number)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        SolidMap map = SolidMap.Build(s_levels.Level(number), s_zones.Zones(number));
        Rebb64Machine machine = Start(number);
        int spawns = 0;

        for (int call = 0; call < Calls; call++)
        {
            Capture capture = machine.Bracket(0x0BED, CallReturn, 300_000);

            byte[] before = capture.Before;
            byte[] after = capture.After;

            ObjectTable objects = new();
            RamImage.Load(before, objects);

            BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
            {
                Low = before[Random],
                High = before[Random + 1],
            };

            BubbleSpawner spawner = new(objects, new EntityTable { EnemyCount = before[EnemyCount] }, random)
            {
                Enabled = true,
                Timer = before[Countdown],
                Standard = before[Chooser] == JmpOpcode,
                Mask = before[MaskOperand],
                LetterCount = before[Letters],
                Seed = before[SeedAddress],
            };

            spawner.Update(map);

            string expected = RamImage.Objects(after);
            string actual = RamImage.Objects(objects);

            Assert.True(expected == actual, $"call {call}: {RamImage.Difference(expected, actual)}; corners {Corners(before, map)}");
            Assert.Equal(after[Countdown], spawner.Timer);
            Assert.Equal(after[Letters], spawner.LetterCount);
            Assert.Equal(after[SeedAddress], spawner.Seed);
            Assert.Equal(after[Chooser] == JmpOpcode, spawner.Standard);
            Assert.Equal(after[Random], random.Low);
            Assert.Equal(after[Random + 1], random.High);

            spawns += RamImage.Objects(before) == RamImage.Objects(after) ? 0 : 1;
        }

        Assert.True(spawns > 0, $"level {number} made no bubble in {Calls} calls, so nothing was compared");
    }

    // $3A6C-$3AB7: the level's flags into `$0A38`, `$0C9C`, `$0CC2` and `$4C`, with the rolls they take.
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(17)]
    [InlineData(68)]
    [InlineData(78)]
    [InlineData(99)]
    public void ConfiguresExactlyAsTheGameDoes(int number)
    {
        Assert.SkipUnless(Rebb64Machine.Available, "rebb64-raw.prg is not available.");

        Rebb64Machine machine = new();
        machine.StartOnePlayer();

        // Level 1 is already running: the tail runs again from the next level's set-up, reached with `EnterLevel`'s poke.
        machine.Poke(0x21, 0x7F);
        machine.Poke(0x10, (byte)(number - 2));
        machine.Cpu.PC = 0x0A6E;

        Capture capture = machine.Bracket(TailStart, TailEnd, 30_000_000);

        byte[] before = capture.Before;
        byte[] after = capture.After;

        BbRandom random = new(new ScriptedRandomSource(capture.Timer.Select(b => (int)b)))
        {
            Low = before[Random],
            High = before[Random + 1],
        };

        BubbleSpawner spawner = new(new ObjectTable(), new EntityTable(), random)
        {
            Standard = before[Chooser] == JmpOpcode,
            Mask = before[MaskOperand],
            LetterCount = before[Letters],
        };

        spawner.Configure(s_levels.Level(number).SpawnFlags);

        Assert.Equal(after[SpawnSwitch] == JsrOpcode, spawner.Enabled);

        if (spawner.Enabled)
        {
            Assert.Equal(after[Chooser] == JmpOpcode, spawner.Standard);
            Assert.Equal(after[MaskOperand], spawner.Mask);

            if (!spawner.Standard)
            {
                Assert.Equal(after[Letters], spawner.LetterCount);
            }
        }

        Assert.Equal(after[Random], random.Low);
        Assert.Equal(after[Random + 1], random.High);
    }

    // The four cells the spawner looks at, as the game holds them and as the port's map has them.
    private static string Corners(byte[] ram, SolidMap map)
    {
        List<string> cells = [];

        foreach ((int row, int column) in new[] { (0, 10), (0, 20), (24, 10), (24, 20) })
        {
            int port = (map[row, column] ? 0x80 : 0) | map.Direction(row, column);

            cells.Add($"({row},{column}) game ${ram[0x8500 + (row * 40) + column]:X2} port ${port:X2}");
        }

        return string.Join(' ', cells);
    }

    private static Rebb64Machine Start(int number)
    {
        Rebb64Machine machine = new();

        machine.StartOnePlayer();
        machine.EnterLevel(number);

        return machine;
    }

    private static string Asset(string name) => Path.Combine(
        AppContext.BaseDirectory,
        "Renditions",
        "BubbleBobbleSharp.Renditions.EightBit",
        "Assets",
        "Levels",
        name);
}
